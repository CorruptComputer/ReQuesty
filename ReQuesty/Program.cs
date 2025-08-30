using System.CommandLine;
using System.CommandLine.Parsing;

namespace ReQuesty;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = ReQuestyHost.GetRootCommand();

        int ret = await CommandLineParser.Parse(rootCommand, args).InvokeAsync();

        DisposeSubCommands(rootCommand);

        return ret;
    }

    private static void DisposeSubCommands(this Command command)
    {
        if (command is IDisposable disposableHandler)
        {
            disposableHandler.Dispose();
        }

        foreach (Command subCommand in command.Subcommands)
        {
            DisposeSubCommands(subCommand);
        }
    }
}
