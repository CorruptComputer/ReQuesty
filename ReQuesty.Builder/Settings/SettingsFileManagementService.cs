using System.Text.Json;
using Microsoft.OpenApi;

namespace ReQuesty.Builder.Settings;

public class SettingsFileManagementService : ISettingsManagementService
{
    internal const string SettingsFileName = "settings.json";
    internal const string EnvironmentVariablesKey = "rest-client.environmentVariables";
    internal const string VsCodeDirectoryName = ".vscode";
    public string GetDirectoryContainingSettingsFile(string searchDirectory)
    {
        DirectoryInfo currentDirectory = new(searchDirectory);
        string vscodeDirectoryPath = Path.Combine(currentDirectory.FullName, VsCodeDirectoryName);
        if (Directory.Exists(vscodeDirectoryPath))
        {
            return vscodeDirectoryPath;
        }
        string pathToWrite = Path.Combine(searchDirectory, VsCodeDirectoryName);
        return Directory.CreateDirectory(pathToWrite).FullName;
    }

    public Task WriteSettingsFileAsync(string directoryPath, OpenApiDocument openApiDocument, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        ArgumentNullException.ThrowIfNull(openApiDocument);
        SettingsFile settings = GenerateSettingsFile(openApiDocument);
        return WriteSettingsFileInternalAsync(directoryPath, settings, cancellationToken);
    }

    private static SettingsFile GenerateSettingsFile(OpenApiDocument openApiDocument)
    {
        SettingsFile settings = new();
        if (openApiDocument.Servers is { Count: > 0 } && openApiDocument.Servers[0] is { Url: { } url })
        {
            settings.EnvironmentVariables.Development.HostAddress = url;
            settings.EnvironmentVariables.Remote.HostAddress = url;
        }
        return settings;
    }

    private async Task WriteSettingsFileInternalAsync(string directoryPath, SettingsFile settings, CancellationToken cancellationToken)
    {
        string? parentDirectoryPath = Path.GetDirectoryName(directoryPath);
        string vscodeDirectoryPath = GetDirectoryContainingSettingsFile(parentDirectoryPath!);
        string settingsObjectString = JsonSerializer.Serialize(settings, SettingsFileGenerationContext.Default.SettingsFile);
        string fileUpdatePath = Path.Combine(vscodeDirectoryPath, SettingsFileName);
        await VsCodeSettingsManager.UpdateFileAsync(settingsObjectString, fileUpdatePath, EnvironmentVariablesKey, cancellationToken).ConfigureAwait(false);
    }
}
