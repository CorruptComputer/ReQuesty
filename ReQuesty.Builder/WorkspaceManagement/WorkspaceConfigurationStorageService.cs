using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AsyncKeyedLock;
using ReQuesty.Builder.Manifest;
using Microsoft.OpenApi.ApiManifest;

namespace ReQuesty.Builder.WorkspaceManagement;

public class WorkspaceConfigurationStorageService
{
    public const string ConfigurationFileName = "workspace.json";
    public const string ManifestFileName = "apimanifest.json";
    public static string ReQuestyDirectorySegment => DescriptionStorageService.ReQuestyDirectorySegment;
    public string TargetDirectory
    {
        get; private set;
    }
    private readonly string targetConfigurationFilePath;
    private readonly string targetManifestFilePath;
    private readonly ManifestManagementService manifestManagementService = new();
    public WorkspaceConfigurationStorageService(string targetDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetDirectory);
        TargetDirectory = Path.Combine(targetDirectory, ReQuestyDirectorySegment);
        targetConfigurationFilePath = Path.Combine(TargetDirectory, ConfigurationFileName);
        targetManifestFilePath = Path.Combine(TargetDirectory, ManifestFileName);
    }
    private static readonly AsyncKeyedLocker<string> localFilesLock = new(o =>
    {
        o.PoolSize = 20;
        o.PoolInitialFill = 1;
    });
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await IsInitializedAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("The workspace configuration already exists");
        }

        await UpdateWorkspaceConfigurationAsync(new WorkspaceConfiguration(), null, cancellationToken).ConfigureAwait(false);
    }
    public async Task UpdateWorkspaceConfigurationAsync(WorkspaceConfiguration configuration, ApiManifestDocument? manifestDocument, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        using (await localFilesLock.LockAsync(targetConfigurationFilePath, cancellationToken).ConfigureAwait(false))
        {
            if (!Directory.Exists(TargetDirectory))
            {
                Directory.CreateDirectory(TargetDirectory);
            }
            await using FileStream configStream = File.Open(targetConfigurationFilePath, FileMode.Create);
            await JsonSerializer.SerializeAsync(configStream, configuration, context.WorkspaceConfiguration, cancellationToken).ConfigureAwait(false);
            if (manifestDocument is not null)
            {
                using (await localFilesLock.LockAsync(targetManifestFilePath, cancellationToken).ConfigureAwait(false))
                {
                    await using FileStream manifestStream = File.Open(targetManifestFilePath, FileMode.Create);
                    await manifestManagementService.SerializeManifestDocumentAsync(manifestDocument, manifestStream).ConfigureAwait(false);
                }
            }
        }
    }
    public Task<bool> IsInitializedAsync(CancellationToken cancellationToken = default)
    {// keeping this as a task in case we want to do more complex validation in the future
        return Task.FromResult(File.Exists(targetConfigurationFilePath));
    }
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
    private static readonly WorkspaceConfigurationGenerationContext context = new(options);
    public async Task<(WorkspaceConfiguration?, ApiManifestDocument?)> GetWorkspaceConfigurationAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(targetConfigurationFilePath))
        {
            using (await localFilesLock.LockAsync(targetConfigurationFilePath, cancellationToken).ConfigureAwait(false))
            {
                await using FileStream configStream = File.OpenRead(targetConfigurationFilePath);
                WorkspaceConfiguration? config = await JsonSerializer.DeserializeAsync(configStream, context.WorkspaceConfiguration, cancellationToken).ConfigureAwait(false);
                if (File.Exists(targetManifestFilePath))
                {
                    using (await localFilesLock.LockAsync(targetManifestFilePath, cancellationToken).ConfigureAwait(false))
                    {
                        await using FileStream manifestStream = File.OpenRead(targetManifestFilePath);
                        ApiManifestDocument? manifest = await manifestManagementService.DeserializeManifestDocumentAsync(manifestStream).ConfigureAwait(false);
                        if (manifest is not null)
                        {
                            foreach (ApiDependency? apiDependency in manifest.ApiDependencies.Select(static x => x.Value))
                            {
                                apiDependency.Requests = apiDependency.Requests.Where(static x => !WorkspaceManagementService.MigrationPlaceholderPath.Equals(x.UriTemplate, StringComparison.Ordinal)).ToList();
                            }
                        }
                        // avoids migrations resulting in empty clients
                        return (config, manifest);
                    }
                }

                return (config, null);
            }
        }

        return (null, null);
    }
    public async Task BackupConfigAsync(CancellationToken cancellationToken = default)
    {
        await BackupFileAsync(ConfigurationFileName, cancellationToken).ConfigureAwait(false);
        await BackupFileAsync(ManifestFileName, cancellationToken).ConfigureAwait(false);
    }
    private async Task BackupFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        string sourceFilePath = Path.Combine(TargetDirectory, fileName);
        if (File.Exists(sourceFilePath))
        {
            string backupFilePath = GetBackupFilePath(TargetDirectory, fileName);
            using (await localFilesLock.LockAsync(backupFilePath, cancellationToken).ConfigureAwait(false))
            {
                string? backupStorageDirectory = Path.GetDirectoryName(backupFilePath);
                if (string.IsNullOrEmpty(backupStorageDirectory))
                {
                    return;
                }

                if (!Directory.Exists(backupStorageDirectory))
                {
                    Directory.CreateDirectory(backupStorageDirectory);
                }

                File.Copy(sourceFilePath, backupFilePath, true);
            }
        }
    }
    public async Task RestoreConfigAsync(CancellationToken cancellationToken = default)
    {
        await RestoreFileAsync(ConfigurationFileName, cancellationToken).ConfigureAwait(false);
        await RestoreFileAsync(ManifestFileName, cancellationToken).ConfigureAwait(false);
    }
    private async Task RestoreFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        string sourceFilePath = Path.Combine(TargetDirectory, fileName);
        string? targetDirectory = Path.GetDirectoryName(sourceFilePath);
        if (string.IsNullOrEmpty(targetDirectory))
        {
            return;
        }

        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        string backupFilePath = GetBackupFilePath(TargetDirectory, fileName);
        if (File.Exists(backupFilePath))
        {
            using (await localFilesLock.LockAsync(sourceFilePath, cancellationToken).ConfigureAwait(false))
            {
                File.Copy(backupFilePath, sourceFilePath, true);
            }
        }
    }
    private static readonly ThreadLocal<HashAlgorithm> HashAlgorithm = new(SHA256.Create);
    private static string GetBackupFilePath(string outputPath, string fileName)
    {
        string hashedPath = Convert.ToHexString((HashAlgorithm.Value ?? throw new InvalidOperationException("unable to get hash algorithm")).ComputeHash(Encoding.UTF8.GetBytes(outputPath))).Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase);
        return Path.Combine(Path.GetTempPath(), Constants.TempDirectoryName, "backup", hashedPath, fileName);
    }
}
