using System.CommandLine;
using System.CommandLine.Invocation;
using ReQuesty.Builder;
using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ReQuesty.Handlers;

internal abstract class BaseReQuestyCommandHandler : ICommandHandler, IDisposable
{
    private HttpClient? _httpClient;
    protected HttpClient httpClient
    {
        get
        {
            _httpClient ??= GetHttpClient();
            return _httpClient;
        }
    }
    public required Option<LogLevel> LogLevelOption
    {
        get; init;
    }
    protected ReQuestyConfiguration Configuration
    {
        get => ConfigurationFactory.Value;
    }
    private readonly Lazy<ReQuestyConfiguration> ConfigurationFactory = new(() =>
    {
        ConfigurationBuilder builder = new();
        using MemoryStream defaultStream = new(ReQuesty.Generated.ReQuestyAppSettings.Default());
        IConfigurationRoot configuration = builder.AddJsonStream(defaultStream)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables(prefix: "REQUSTY_")
                .Build();
        ReQuestyConfiguration configObject = new();
        configObject.BindConfiguration(configuration);
        return configObject;
    });

    protected HttpClient GetHttpClient()
    {
        HttpClientHandler httpClientHandler = new();
        if (Configuration.Generation.DisableSSLValidation)
        {
            httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        HttpClient httpClient = new(httpClientHandler);

        disposables.Add(httpClientHandler);
        disposables.Add(httpClient);

        return httpClient;
    }

    public int Invoke(InvocationContext context)
    {
        throw new InvalidOperationException("This command handler is async only");
    }

    public abstract Task<int> InvokeAsync(InvocationContext context);
    private readonly List<IDisposable> disposables = [];
    protected (ILoggerFactory, ILogger<T>) GetLoggerAndFactory<T>(InvocationContext context, string logFileRootPath = "")
    {
        LogLevel logLevel = context.ParseResult.GetValueForOption(LogLevelOption);
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            string logFileAbsoluteRootPath = GetAbsolutePath(logFileRootPath);
            FileLogLoggerProvider fileLogger = new(logFileAbsoluteRootPath, logLevel);
            disposables.Add(fileLogger);
            builder
                .AddConsole()
#if DEBUG
                .AddDebug()
#endif
                .AddProvider(fileLogger)
                .SetMinimumLevel(logLevel);
        });
        ILogger<T> logger = loggerFactory.CreateLogger<T>();
        return (loggerFactory, logger);
    }
    protected static string GetAbsolutePath(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.Empty;
        }

        return Path.IsPathRooted(source) || source.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? source : NormalizeSlashesInPath(Path.Combine(Directory.GetCurrentDirectory(), source));
    }
    protected void AssignIfNotNullOrEmpty(string? input, Action<GenerationConfiguration, string> assignment)
    {
        if (!string.IsNullOrEmpty(input))
        {
            assignment.Invoke(Configuration.Generation, input);
        }
    }
    protected static string NormalizeSlashesInPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            return path.Replace('/', '\\');
        }

        return path.Replace('\\', '/');
    }
    private readonly Lazy<bool> consoleSwapColors = new(() =>
    {
        string? requestySwapColorsRaw = Environment.GetEnvironmentVariable("REQUESTY_CONSOLE_COLORS_SWAP");
        if (!string.IsNullOrEmpty(requestySwapColorsRaw) && bool.TryParse(requestySwapColorsRaw, out bool requestySwapColors))
        {
            return requestySwapColors;
        }
        return false;
    });
    protected bool SwapColors => consoleSwapColors.Value;
    private readonly Lazy<bool> consoleNoColors = new(() =>
    {
        string? requestyNoColorsRaw = Environment.GetEnvironmentVariable("REQUESTY_CONSOLE_COLORS_ENABLED");
        if (!string.IsNullOrEmpty(requestyNoColorsRaw) && bool.TryParse(requestyNoColorsRaw, out bool requestyNoColors))
        {
            return requestyNoColors;
        }
        return true;
    });
    protected bool ColorsEnabled => consoleNoColors.Value;

    private void DisplayMessages(ConsoleColor color, params string[] messages)
    {
        if (SwapColors)
        {
            color = Enum.GetValues<ConsoleColor>()[ConsoleColor.White - color];
        }

        if (ColorsEnabled)
        {
            Console.ForegroundColor = color;
        }

        foreach (string message in messages)
        {
            Console.WriteLine(message);
        }

        if (ColorsEnabled)
        {
            Console.ResetColor();
        }
    }
    protected void DisplayError(params string[] messages)
    {
        DisplayMessages(ConsoleColor.Red, messages);
    }
    protected void DisplayWarning(params string[] messages)
    {
        DisplayMessages(ConsoleColor.Yellow, messages);
    }
    protected void DisplaySuccess(params string[] messages)
    {
        DisplayMessages(ConsoleColor.Green, messages);
    }
    protected void DisplayInfo(params string[] messages)
    {
        DisplayMessages(ConsoleColor.White, messages);
    }

    protected void DisplayUrlInformation(string? apiRootUrl, bool isPlugin = false)
    {
        if (!string.IsNullOrEmpty(apiRootUrl) && !isPlugin)
        {
            DisplayInfo($"Client base url set to {apiRootUrl}");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        foreach (IDisposable disposable in disposables)
        {
            disposable.Dispose();
        }
    }
}
