using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ReQuesty;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = ReQuestyHost.GetRootCommand();
        Parser parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseHost(static args =>
            {
                return Host.CreateDefaultBuilder(args)
                    .ConfigureLogging(static logging =>
                    {
                        logging.ClearProviders();
#if DEBUG
                        logging.AddDebug();
#endif
                        logging.AddEventSourceLogger();
                    });
            })
            .Build();
        int result = await parser.InvokeAsync(args);
        DisposeSubCommands(rootCommand);
        return result;
    }

    private static void DisposeSubCommands(this Command command)
    {
        if (command.Handler is IDisposable disposableHandler)
        {
            disposableHandler.Dispose();
        }

        foreach (Command subCommand in command.Subcommands)
        {
            DisposeSubCommands(subCommand);
        }
    }
}
