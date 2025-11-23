using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Extensions;
using ReQuesty.Builder.Lock;
using ReQuesty.Builder.Manifest;
using Microsoft.Extensions.Logging;
using ReQuesty.Runtime.Extensions;
using Microsoft.OpenApi.ApiManifest;
using ReQuesty.Core.Logging;

namespace ReQuesty.Builder.WorkspaceManagement;

public class WorkspaceManagementService
{
    private readonly bool UseReQuestyConfig;
    private readonly ILogger Logger;
    public WorkspaceManagementService(ILogger logger, HttpClient httpClient, bool useReQuestyConfig = false, string workingDirectory = "")
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(httpClient);
        Logger = logger;
        UseReQuestyConfig = useReQuestyConfig;
        if (string.IsNullOrEmpty(workingDirectory))
        {
            workingDirectory = Directory.GetCurrentDirectory();
        }

        WorkingDirectory = workingDirectory;
        workspaceConfigurationStorageService = new(workingDirectory);
        descriptionStorageService = new(workingDirectory);
        openApiDocumentDownloadService = new(httpClient, Logger);
    }
    private readonly OpenApiDocumentDownloadService openApiDocumentDownloadService;
    private readonly LockManagementService lockManagementService = new();
    private readonly WorkspaceConfigurationStorageService workspaceConfigurationStorageService;
    private readonly DescriptionStorageService descriptionStorageService;
    public async Task<bool> IsConsumerPresentAsync(string clientName, CancellationToken cancellationToken = default)
    {
        if (!UseReQuestyConfig)
        {
            return false;
        }

        (WorkspaceConfiguration? wsConfig, ApiManifestDocument? _) = await workspaceConfigurationStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
        return wsConfig is not null && (wsConfig.Clients.ContainsKey(clientName) || wsConfig.Plugins.ContainsKey(clientName));
    }
    private BaseApiConsumerConfiguration UpdateConsumerConfiguration(GenerationConfiguration generationConfiguration, WorkspaceConfiguration wsConfig)
    {
        if (generationConfiguration.IsPluginConfiguration)
        {
            ApiPluginConfiguration generationPluginConfig = new(generationConfiguration);
            generationPluginConfig.NormalizeOutputPath(WorkingDirectory);
            generationPluginConfig.NormalizeDescriptionLocation(WorkingDirectory);
            wsConfig.Plugins.AddOrReplace(generationConfiguration.ClientClassName, generationPluginConfig);
            return generationPluginConfig;
        }
        else
        {
            ApiClientConfiguration generationClientConfig = new(generationConfiguration);
            generationClientConfig.NormalizeOutputPath(WorkingDirectory);
            generationClientConfig.NormalizeDescriptionLocation(WorkingDirectory);
            wsConfig.Clients.AddOrReplace(generationConfiguration.ClientClassName, generationClientConfig);
            return generationClientConfig;
        }
    }
    public async Task UpdateStateFromConfigurationAsync(GenerationConfiguration generationConfiguration, string descriptionHash, Dictionary<string, HashSet<string>> templatesWithOperations, Stream descriptionStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(generationConfiguration);
        if (UseReQuestyConfig)
        {
            (WorkspaceConfiguration wsConfig, ApiManifestDocument manifest) = await LoadConfigurationAndManifestAsync(cancellationToken).ConfigureAwait(false);
            BaseApiConsumerConfiguration generationClientConfig = UpdateConsumerConfiguration(generationConfiguration, wsConfig);
            generationClientConfig.NormalizeDescriptionLocation(WorkingDirectory);
            string inputConfigurationHash = await GetConsumerConfigurationHashAsync(generationClientConfig, descriptionHash).ConfigureAwait(false);
            manifest.ApiDependencies.AddOrReplace(generationConfiguration.ClientClassName, generationConfiguration.ToApiDependency(inputConfigurationHash, templatesWithOperations, WorkingDirectory));
            await workspaceConfigurationStorageService.UpdateWorkspaceConfigurationAsync(wsConfig, manifest, cancellationToken).ConfigureAwait(false);
            if (descriptionStream != Stream.Null)
            {
                await descriptionStorageService.UpdateDescriptionAsync(generationConfiguration.ClientClassName, descriptionStream, generationConfiguration.OpenAPIFilePath.GetFileExtension(), cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            ReQuestyLock configurationLock = new(generationConfiguration)
            {
                DescriptionHash = descriptionHash ?? string.Empty,
            };
            await lockManagementService.WriteLockFileAsync(generationConfiguration.OutputPath, configurationLock, cancellationToken).ConfigureAwait(false);
        }
    }
    public async Task RestoreStateAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        if (UseReQuestyConfig)
        {
            await workspaceConfigurationStorageService.RestoreConfigAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await lockManagementService.RestoreLockFileAsync(outputPath, cancellationToken).ConfigureAwait(false);
        }
    }
    public async Task BackupStateAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        if (UseReQuestyConfig)
        {
            await workspaceConfigurationStorageService.BackupConfigAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await lockManagementService.BackupLockFileAsync(outputPath, cancellationToken).ConfigureAwait(false);
        }
    }
    private static readonly ReQuestyLockComparer lockComparer = new();
    private static readonly ApiClientConfigurationComparer clientConfigurationComparer = new();
    private static readonly ApiPluginConfigurationComparer pluginConfigurationComparer = new();
    private static readonly ApiDependencyComparer apiDependencyComparer = new();
    public async Task<bool> ShouldGenerateAsync(GenerationConfiguration inputConfig, string descriptionHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputConfig);
        if (inputConfig.CleanOutput)
        {
            return true;
        }

        if (UseReQuestyConfig)
        {
            (WorkspaceConfiguration? wsConfig, ApiManifestDocument? apiManifest) = await workspaceConfigurationStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
            if (wsConfig is null || apiManifest is null)
            {
                return true;
            }

            if (wsConfig.Clients.TryGetValue(inputConfig.ClientClassName, out ApiClientConfiguration? existingClientConfig) &&
                apiManifest.ApiDependencies.TryGetValue(inputConfig.ClientClassName, out ApiDependency? existingApiManifest))
            {
                ApiClientConfiguration inputClientConfig = new(inputConfig);
                inputClientConfig.NormalizeOutputPath(WorkingDirectory);
                inputClientConfig.NormalizeDescriptionLocation(WorkingDirectory);
                string inputConfigurationHash = await GetConsumerConfigurationHashAsync(inputClientConfig, descriptionHash).ConfigureAwait(false);
                return !clientConfigurationComparer.Equals(existingClientConfig, inputClientConfig) ||
                       !apiDependencyComparer.Equals(inputConfig.ToApiDependency(inputConfigurationHash, [], WorkingDirectory), existingApiManifest);
            }
            if (wsConfig.Plugins.TryGetValue(inputConfig.ClientClassName, out ApiPluginConfiguration? existingPluginConfig) &&
                apiManifest.ApiDependencies.TryGetValue(inputConfig.ClientClassName, out ApiDependency? existingPluginApiManifest))
            {
                ApiPluginConfiguration inputClientConfig = new(inputConfig);
                inputClientConfig.NormalizeOutputPath(WorkingDirectory);
                inputClientConfig.NormalizeDescriptionLocation(WorkingDirectory);
                string inputConfigurationHash = await GetConsumerConfigurationHashAsync(inputClientConfig, descriptionHash).ConfigureAwait(false);
                return !pluginConfigurationComparer.Equals(existingPluginConfig, inputClientConfig) ||
                       !apiDependencyComparer.Equals(inputConfig.ToApiDependency(inputConfigurationHash, [], WorkingDirectory), existingPluginApiManifest);
            }
            return true;
        }
        else
        {
            ReQuestyLock? existingLock = await lockManagementService.GetLockFromDirectoryAsync(inputConfig.OutputPath, cancellationToken).ConfigureAwait(false);
            ReQuestyLock configurationLock = new(inputConfig)
            {
                DescriptionHash = descriptionHash,
            };
            if (!string.IsNullOrEmpty(existingLock?.ReQuestyVersion) && !configurationLock.ReQuestyVersion.Equals(existingLock.ReQuestyVersion, StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning("API client was generated with version {ExistingVersion} and the current version is {CurrentVersion}, it will be upgraded and you should upgrade dependencies", existingLock.ReQuestyVersion, configurationLock.ReQuestyVersion);
            }
            return !lockComparer.Equals(existingLock, configurationLock);
        }

    }
    public async Task<Stream?> GetDescriptionCopyAsync(string clientName, string inputPath, bool cleanOutput, CancellationToken cancellationToken = default)
    {
        if (!UseReQuestyConfig || cleanOutput)
        {
            return null;
        }

        return await descriptionStorageService.GetDescriptionAsync(clientName, new Uri(inputPath).GetFileExtension(), cancellationToken).ConfigureAwait(false);
    }
    public Task RemoveClientAsync(string clientName, bool cleanOutput = false, CancellationToken cancellationToken = default)
    {
        return RemoveConsumerInternalAsync(clientName,
            static wsConfig => wsConfig.Clients,
            cleanOutput,
            "client",
            cancellationToken
        );
    }
    public Task RemovePluginAsync(string clientName, bool cleanOutput = false, CancellationToken cancellationToken = default)
    {
        return RemoveConsumerInternalAsync(clientName,
            static wsConfig => wsConfig.Plugins,
            cleanOutput,
            "plugin",
            cancellationToken
        );
    }
    private async Task RemoveConsumerInternalAsync<T>(string consumerName, Func<WorkspaceConfiguration, Dictionary<string, T>> consumerRetrieval, bool cleanOutput, string consumerDisplayName, CancellationToken cancellationToken) where T : BaseApiConsumerConfiguration
    {
        if (!UseReQuestyConfig)
        {
            throw new InvalidOperationException($"Cannot remove a {consumerDisplayName} in lock mode");
        }

        (WorkspaceConfiguration? wsConfig, ApiManifestDocument? manifest) = await workspaceConfigurationStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
        if (wsConfig is null)
        {
            throw new InvalidOperationException($"Cannot remove a {consumerDisplayName} without a configuration");
        }

        Dictionary<string, T> consumers = consumerRetrieval(wsConfig);
        if (cleanOutput && consumers.TryGetValue(consumerName, out T? consumerConfig) && Directory.Exists(consumerConfig.OutputPath))
        {
            Directory.Delete(consumerConfig.OutputPath, true);
        }

        if (!consumers.Remove(consumerName))
        {
            throw new InvalidOperationException($"The {consumerDisplayName} {consumerName} was not found in the configuration");
        }

        manifest?.ApiDependencies.Remove(consumerName);
        await workspaceConfigurationStorageService.UpdateWorkspaceConfigurationAsync(wsConfig, manifest, cancellationToken).ConfigureAwait(false);
        descriptionStorageService.RemoveDescription(consumerName);
        if (!wsConfig.AnyConsumerPresent)
        {
            descriptionStorageService.Clean();
        }
    }
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
    private static readonly WorkspaceConfigurationGenerationContext context = new(options);
    private static readonly ThreadLocal<HashAlgorithm> HashAlgorithm = new(SHA256.Create);
    private readonly string WorkingDirectory;
    private async Task<string> GetConsumerConfigurationHashAsync<T>(T apiClientConfiguration, string descriptionHash) where T : BaseApiConsumerConfiguration
    {
        using MemoryStream stream = new();
        if (apiClientConfiguration is ApiClientConfiguration)
        {
            await JsonSerializer.SerializeAsync(stream, apiClientConfiguration, context.ApiClientConfiguration).ConfigureAwait(false);
        }
        else
        {
            await JsonSerializer.SerializeAsync(stream, apiClientConfiguration, context.ApiPluginConfiguration).ConfigureAwait(false);
        }

        await stream.WriteAsync(Encoding.UTF8.GetBytes(descriptionHash)).ConfigureAwait(false);
        stream.Position = 0;
        if (HashAlgorithm.Value is null)
        {
            throw new InvalidOperationException("Hash algorithm is not available");
        }

        return ConvertByteArrayToString(await HashAlgorithm.Value.ComputeHashAsync(stream).ConfigureAwait(false));
    }
    private static string ConvertByteArrayToString(byte[] hash)
    {
        // Build the final string by converting each byte
        // into hex and appending it to a StringBuilder
        int sbLength = hash.Length * 2;
        StringBuilder sb = new(sbLength, sbLength);
        for (int i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }
    private async Task<(WorkspaceConfiguration, ApiManifestDocument)> LoadConfigurationAndManifestAsync(CancellationToken cancellationToken)
    {
        if (!await workspaceConfigurationStorageService.IsInitializedAsync(cancellationToken).ConfigureAwait(false))
        {
            await workspaceConfigurationStorageService.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        (WorkspaceConfiguration? wsConfig, ApiManifestDocument? apiManifest) = await workspaceConfigurationStorageService.GetWorkspaceConfigurationAsync(cancellationToken).ConfigureAwait(false);
        if (wsConfig is null)
        {
            throw new InvalidOperationException("The workspace configuration is not initialized");
        }

        apiManifest ??= new("application"); //TODO get the application name
        return (wsConfig, apiManifest);
    }
    private async Task<List<GenerationConfiguration>> LoadGenerationConfigurationsFromLockFilesAsync(string lockDirectory, string clientName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(lockDirectory);
        if (!UseReQuestyConfig)
        {
            throw new InvalidOperationException("Cannot migrate from lock file in requesty config mode");
        }

        if (!Path.IsPathRooted(lockDirectory))
        {
            lockDirectory = Path.Combine(WorkingDirectory, lockDirectory);
        }

        if (Path.GetRelativePath(WorkingDirectory, lockDirectory).StartsWith("..", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The lock directory must be a subdirectory of the working directory");
        }

        string[] lockFiles = Directory.GetFiles(lockDirectory, LockManagementService.LockFileName, SearchOption.AllDirectories);
        if (lockFiles.Length == 0)
        {
            throw new InvalidOperationException("No lock file found in the specified directory");
        }

        bool clientNamePassed = !string.IsNullOrEmpty(clientName);
        if (lockFiles.Length > 1 && clientNamePassed)
        {
            throw new InvalidOperationException("Multiple lock files found in the specified directory and the client name was specified");
        }

        List<GenerationConfiguration?> clientsGenerationConfigurations = [];
        if (lockFiles.Length == 1)
        {
            clientsGenerationConfigurations.Add(await LoadConfigurationFromLockAsync(clientNamePassed ? clientName : string.Empty, lockFiles[0], cancellationToken).ConfigureAwait(false));
        }
        else
        {
            clientsGenerationConfigurations.AddRange(await Task.WhenAll(lockFiles.Select(x => LoadConfigurationFromLockAsync(string.Empty, x, cancellationToken))).ConfigureAwait(false));
        }

        return clientsGenerationConfigurations.OfType<GenerationConfiguration>().ToList();
    }
    public async Task<IEnumerable<string>> MigrateFromLockFileAsync(string clientName, string lockDirectory, CancellationToken cancellationToken = default)
    {
        (WorkspaceConfiguration wsConfig, ApiManifestDocument apiManifest) = await LoadConfigurationAndManifestAsync(cancellationToken).ConfigureAwait(false);

        List<GenerationConfiguration> clientsGenerationConfigurations = await LoadGenerationConfigurationsFromLockFilesAsync(lockDirectory, clientName, cancellationToken).ConfigureAwait(false);
        foreach (GenerationConfiguration generationConfiguration in clientsGenerationConfigurations.ToArray()) //to avoid modifying the collection as we iterate and remove some entries
        {

            if (wsConfig.Clients.ContainsKey(generationConfiguration.ClientClassName))
            {
                Logger.DuplicateClientNameError(generationConfiguration.ClientClassName);
                clientsGenerationConfigurations.Remove(generationConfiguration);
                continue;
            }
            (Stream stream, bool _) = await openApiDocumentDownloadService.LoadStreamAsync(generationConfiguration.OpenAPIFilePath, generationConfiguration, null, false, cancellationToken).ConfigureAwait(false);
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
            await using MemoryStream ms = new();
            await using MemoryStream msForParsing = new();
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
            await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);
            await ms.CopyToAsync(msForParsing, cancellationToken).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);
            // OpenAPI.net or STJ disposes the stream, working on a copy avoids a stream disposed exception
            msForParsing.Seek(0, SeekOrigin.Begin);
            Microsoft.OpenApi.OpenApiDocument? document = await openApiDocumentDownloadService.GetDocumentFromStreamAsync(msForParsing, generationConfiguration, false, cancellationToken).ConfigureAwait(false);
            if (document is null)
            {
                Logger.ClientFailedToMigrateDueToMissingOpenApiDoc(generationConfiguration.ClientClassName);
                clientsGenerationConfigurations.Remove(generationConfiguration);
                continue;
            }
            generationConfiguration.ApiRootUrl = document.GetAPIRootUrl(generationConfiguration.OpenAPIFilePath);
            await descriptionStorageService.UpdateDescriptionAsync(generationConfiguration.ClientClassName, ms, new Uri(generationConfiguration.OpenAPIFilePath).GetFileExtension(), cancellationToken).ConfigureAwait(false);

            ApiClientConfiguration clientConfiguration = new(generationConfiguration);
            clientConfiguration.NormalizeOutputPath(WorkingDirectory);
            clientConfiguration.NormalizeDescriptionLocation(WorkingDirectory);
            wsConfig.Clients.Add(generationConfiguration.ClientClassName, clientConfiguration);
            string inputConfigurationHash = await GetConsumerConfigurationHashAsync(clientConfiguration, "migrated-pending-generate").ConfigureAwait(false);
            // because it's a migration, we don't want to calculate the exact hash since the description might have changed since the initial generation that created the lock file
            apiManifest.ApiDependencies.Add(
                generationConfiguration.ClientClassName,
                generationConfiguration.ToApiDependency(
                    inputConfigurationHash,
                    new Dictionary<string, HashSet<string>> {
                        { MigrationPlaceholderPath, new HashSet<string> { "GET" } }
                    },
                    WorkingDirectory));
            lockManagementService.DeleteLockFile(Path.Combine(WorkingDirectory, clientConfiguration.OutputPath));
        }
        await workspaceConfigurationStorageService.UpdateWorkspaceConfigurationAsync(wsConfig, apiManifest, cancellationToken).ConfigureAwait(false);
        return clientsGenerationConfigurations.OfType<GenerationConfiguration>().Select(static x => x.ClientClassName);
    }
    internal const string MigrationPlaceholderPath = "/migration-placeholder";
    private async Task<GenerationConfiguration?> LoadConfigurationFromLockAsync(string clientName, string lockFilePath, CancellationToken cancellationToken)
    {
        if (Path.GetDirectoryName(lockFilePath) is not string lockFileDirectory)
        {
            Logger.LogWarning("The lock file {LockFilePath} is not in a directory, it will be skipped", lockFilePath);
            return null;
        }
        ReQuestyLock? lockInfo = await lockManagementService.GetLockFromDirectoryAsync(lockFileDirectory, cancellationToken).ConfigureAwait(false);
        if (lockInfo is null)
        {
            Logger.LogWarning("The lock file {LockFilePath} is not valid, it will be skipped", lockFilePath);
            return null;
        }
        GenerationConfiguration generationConfiguration = new();
        lockInfo.UpdateGenerationConfigurationFromLock(generationConfiguration);
        generationConfiguration.OutputPath = "./" + Path.GetRelativePath(WorkingDirectory, lockFileDirectory).NormalizePathSeparators();
        if (!string.IsNullOrEmpty(clientName))
        {
            generationConfiguration.ClientClassName = clientName;
        }
        return generationConfiguration;
    }
}
