using System.Diagnostics;
using System.Security;
using AsyncKeyedLock;
using ReQuesty.Builder.Caching;
using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Extensions;
using ReQuesty.Builder.Validation;
using ReQuesty.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace ReQuesty.Builder;

internal class OpenApiDocumentDownloadService
{
    private readonly ILogger Logger;
    private readonly HttpClient HttpClient;
    public OpenApiDocumentDownloadService(HttpClient httpClient, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);
        HttpClient = httpClient;
        Logger = logger;
    }
    private static readonly AsyncKeyedLocker<string> localFilesLock = new(o =>
    {
        o.PoolSize = 20;
        o.PoolInitialFill = 1;
    });
    internal async Task<(Stream, bool)> LoadStreamAsync(string inputPath, GenerationConfiguration config, WorkspaceManagementService? workspaceManagementService = default, bool useReQuestyConfig = false, CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        inputPath = inputPath.Trim();

        Stream input;
        bool isDescriptionFromWorkspaceCopy = false;
        if (useReQuestyConfig &&
            config.Operation is ConsumerOperation.Edit or ConsumerOperation.Add &&
            workspaceManagementService is not null &&
            await workspaceManagementService.GetDescriptionCopyAsync(config.ClientClassName, inputPath, config.CleanOutput, cancellationToken).ConfigureAwait(false) is { } descriptionStream)
        {
            Logger.LogInformation("loaded description from the workspace copy");
            input = descriptionStream;
            isDescriptionFromWorkspaceCopy = true;
        }
        else if (inputPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                DocumentCachingProvider cachingProvider = new(HttpClient, Logger)
                {
                    ClearCache = config.ClearCache,
                };
                Uri targetUri = new(inputPath);
                string fileName = targetUri.GetFileName() is string name && !string.IsNullOrEmpty(name) ? name : "description.yml";
                input = await cachingProvider.GetDocumentAsync(targetUri, "generation", fileName, cancellationToken: cancellationToken).ConfigureAwait(false);
                Logger.LogInformation("loaded description from remote source");
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Could not download the file at {inputPath}, reason: {ex.Message}", ex);
            }
        }
        else
        {
            try
            {
                MemoryStream inMemoryStream = new();
                using (await localFilesLock.LockAsync(inputPath, cancellationToken).ConfigureAwait(false))
                {// To avoid deadlocking on update with multiple clients for the same local description
                    using FileStream fileStream = new(inputPath, FileMode.Open);
                    await fileStream.CopyToAsync(inMemoryStream, cancellationToken).ConfigureAwait(false);
                }
                inMemoryStream.Position = 0;
                input = inMemoryStream;
                Logger.LogInformation("loaded description from local source");
            }
            catch (Exception ex) when (ex is FileNotFoundException ||
                ex is PathTooLongException ||
                ex is DirectoryNotFoundException ||
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is SecurityException ||
                ex is NotSupportedException)
            {
                throw new InvalidOperationException($"Could not open the file at {inputPath}, reason: {ex.Message}", ex);
            }
        }

        stopwatch.Stop();
        Logger.LogTrace("{Timestamp}ms: Read OpenAPI file {File}", stopwatch.ElapsedMilliseconds, inputPath);
        return (input, isDescriptionFromWorkspaceCopy);
    }

    internal async Task<ReadResult?> GetDocumentWithResultFromStreamAsync(Stream input, GenerationConfiguration config, bool generating = false, CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();
        Logger.LogTrace("Parsing OpenAPI file");
        ValidationRuleSet ruleSet = config.DisabledValidationRules.Contains(ValidationRuleSetExtensions.AllValidationRule) ?
                    ValidationRuleSet.GetEmptyRuleSet() :
                    ValidationRuleSet.GetDefaultRuleSet(); //workaround since validation rule set doesn't support clearing rules
        bool generatingMode = generating || config.IncludeReQuestyValidationRules == true;
        if (generatingMode)
        {
            ruleSet.AddReQuestyValidationRules(config);
        }

        OpenApiReaderSettings settings = new()
        {
            RuleSet = ruleSet,
            LoadExternalRefs = true,
            LeaveStreamOpen = true,
        };

        // Add all extensions for generation
        settings.AddGenerationExtensions();
        settings.AddYamlReader();
        // Add plugins extensions to parse from the OpenAPI file
        bool addPluginsExtensions = config.IsPluginConfiguration || config.IncludePluginExtensions == true;
        if (addPluginsExtensions)
        {
            settings.AddPluginsExtensions();// Add all extensions for plugins
        }

        try
        {
            string rawUri = config.OpenAPIFilePath.TrimEnd(ReQuestyBuilder.ForwardSlash);
            int lastSlashIndex = rawUri.LastIndexOf(ReQuestyBuilder.ForwardSlash);
            if (lastSlashIndex < 0)
            {
                lastSlashIndex = rawUri.Length - 1;
            }

            Uri documentUri = new(rawUri[..lastSlashIndex]);
            settings.BaseUrl = documentUri;
        }
#pragma warning disable CA1031
        catch
#pragma warning restore CA1031
        {
            // couldn't parse the URL, it's probably a local file
        }
        ReadResult readResult = await OpenApiDocument.LoadAsync(input, settings: settings, cancellationToken: cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();
        if (generatingMode && readResult.Diagnostic?.Warnings is { Count: > 0 })
        {
            foreach (OpenApiError warning in readResult.Diagnostic.Warnings)
            {
                Logger.LogWarning("OpenAPI warning: {Pointer} - {Warning}", warning.Pointer, warning.Message);
            }
        }

        if (readResult.Diagnostic?.Errors is { Count: > 0 })
        {
            Logger.LogTrace("{Timestamp}ms: Parsed OpenAPI with errors. {Count} paths found.", stopwatch.ElapsedMilliseconds, readResult.Document?.Paths?.Count ?? 0);
            foreach (OpenApiError parsingError in readResult.Diagnostic.Errors)
            {
                Logger.LogError("OpenAPI error: {Pointer} - {Message}", parsingError.Pointer, parsingError.Message);
            }
        }
        else
        {
            Logger.LogTrace("{Timestamp}ms: Parsed OpenAPI successfully. {Count} paths found.", stopwatch.ElapsedMilliseconds, readResult.Document?.Paths?.Count ?? 0);
        }

        return readResult;
    }

    internal async Task<OpenApiDocument?> GetDocumentFromStreamAsync(Stream input, GenerationConfiguration config, bool generating = false, CancellationToken cancellationToken = default)
    {
        ReadResult? result = await GetDocumentWithResultFromStreamAsync(input, config, generating, cancellationToken).ConfigureAwait(false);
        return result?.Document;
    }
}
