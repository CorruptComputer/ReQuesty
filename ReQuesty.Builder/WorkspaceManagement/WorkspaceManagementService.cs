using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Extensions;
using ReQuesty.Builder.Lock;
using Microsoft.Extensions.Logging;
using ReQuesty.Core.Logging;

namespace ReQuesty.Builder.WorkspaceManagement;

public class WorkspaceManagementService
{
    private readonly ILogger Logger;
    public WorkspaceManagementService(ILogger logger, string workingDirectory = "")
    {
        ArgumentNullException.ThrowIfNull(logger);
        Logger = logger;
        if (string.IsNullOrEmpty(workingDirectory))
        {
            workingDirectory = Directory.GetCurrentDirectory();
        }

        WorkingDirectory = workingDirectory;
    }
    private readonly LockManagementService lockManagementService = new();
    private readonly string WorkingDirectory;
    private static readonly ReQuestyLockComparer lockComparer = new();
    public async Task<bool> ShouldGenerateAsync(GenerationConfiguration inputConfig, string descriptionHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputConfig);
        if (inputConfig.CleanOutput)
        {
            return true;
        }

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
    public async Task UpdateStateFromConfigurationAsync(GenerationConfiguration generationConfiguration, string descriptionHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(generationConfiguration);
        ReQuestyLock configurationLock = new(generationConfiguration)
        {
            DescriptionHash = descriptionHash ?? string.Empty,
        };
        await lockManagementService.WriteLockFileAsync(generationConfiguration.OutputPath, configurationLock, cancellationToken).ConfigureAwait(false);
    }
    public Task RestoreStateAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        return lockManagementService.RestoreLockFileAsync(outputPath, cancellationToken);
    }
    public Task BackupStateAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        return lockManagementService.BackupLockFileAsync(outputPath, cancellationToken);
    }
}
