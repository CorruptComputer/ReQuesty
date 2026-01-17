using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ReQuesty.Builder.Extensions;

namespace ReQuesty.Builder.Lock;

/// <summary>
/// A service that manages the lock file for a ReQuesty project implemented using the file system.
/// </summary>
public class LockManagementService : ILockManagementService
{
    internal const string LockFileName = "requesty-lock.json";
    /// <inheritdoc/>
    public IEnumerable<string> GetDirectoriesContainingLockFile(string searchDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(searchDirectory);
        string[] files = Directory.GetFiles(searchDirectory, LockFileName, SearchOption.AllDirectories);
        return files.Select(Path.GetDirectoryName).Where(x => !string.IsNullOrEmpty(x)).OfType<string>();
    }
    /// <inheritdoc/>
    public Task<ReQuestyLock?> GetLockFromDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        return GetLockFromDirectoryInternalAsync(directoryPath, cancellationToken);
    }
    private static async Task<ReQuestyLock?> GetLockFromDirectoryInternalAsync(string directoryPath, CancellationToken cancellationToken)
    {
        string lockFilePath = Path.Combine(directoryPath, LockFileName);
        if (File.Exists(lockFilePath))
        {
            await using FileStream fileStream = File.OpenRead(lockFilePath);
            ReQuestyLock? result = await GetLockFromStreamInternalAsync(fileStream, cancellationToken).ConfigureAwait(false);
            if (result is not null && IsDescriptionLocal(result.DescriptionLocation) && !Path.IsPathRooted(result.DescriptionLocation))
            {
                result.DescriptionLocation = Path.GetFullPath(Path.Combine(directoryPath, result.DescriptionLocation));
            }
            return result;
        }
        return null;
    }
    /// <inheritdoc/>
    public Task<ReQuestyLock?> GetLockFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return GetLockFromStreamInternalAsync(stream, cancellationToken);
    }
    private static async Task<ReQuestyLock?> GetLockFromStreamInternalAsync(Stream stream, CancellationToken cancellationToken)
    {
        return await JsonSerializer.DeserializeAsync(stream, context.ReQuestyLock, cancellationToken).ConfigureAwait(false);
    }
    /// <inheritdoc/>
    public Task WriteLockFileAsync(string directoryPath, ReQuestyLock lockInfo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        ArgumentNullException.ThrowIfNull(lockInfo);
        return WriteLockFileInternalAsync(directoryPath, lockInfo, cancellationToken);
    }
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
    private static readonly ReQuestyLockGenerationContext context = new(options);
    private static async Task WriteLockFileInternalAsync(string directoryPath, ReQuestyLock lockInfo, CancellationToken cancellationToken)
    {
        string lockFilePath = Path.Combine(directoryPath, LockFileName);
        await using FileStream fileStream = File.Open(lockFilePath, FileMode.Create);
        lockInfo.DescriptionLocation = GetRelativeDescriptionPath(lockInfo.DescriptionLocation, lockFilePath);
        await JsonSerializer.SerializeAsync(fileStream, lockInfo, context.ReQuestyLock, cancellationToken).ConfigureAwait(false);
    }
    private static bool IsDescriptionLocal(string descriptionPath) => !descriptionPath.StartsWith("http", StringComparison.OrdinalIgnoreCase);
    private static string GetRelativeDescriptionPath(string descriptionPath, string lockFilePath)
    {
        if (IsDescriptionLocal(descriptionPath) &&
            Path.GetDirectoryName(lockFilePath) is string lockFileDirectoryPath)
        {
            return Path.GetRelativePath(lockFileDirectoryPath, descriptionPath).NormalizePathSeparators();
        }

        return descriptionPath;
    }
    /// <inheritdoc/>
    public Task BackupLockFileAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        return BackupLockFileInternalAsync(directoryPath);
    }
    private static Task BackupLockFileInternalAsync(string directoryPath)
    {
        string lockFilePath = Path.Combine(directoryPath, LockFileName);
        if (File.Exists(lockFilePath))
        {
            string backupFilePath = GetBackupFilePath(directoryPath);
            string? targetDirectory = Path.GetDirectoryName(backupFilePath);
            if (string.IsNullOrEmpty(targetDirectory))
            {
                return Task.CompletedTask;
            }

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.Copy(lockFilePath, backupFilePath, true);
        }
        return Task.CompletedTask;
    }
    /// <inheritdoc/>
    public Task RestoreLockFileAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        return RestoreLockFileInternalAsync(directoryPath);
    }
    private static Task RestoreLockFileInternalAsync(string directoryPath)
    {
        string lockFilePath = Path.Combine(directoryPath, LockFileName);
        string? targetDirectory = Path.GetDirectoryName(lockFilePath);
        if (string.IsNullOrEmpty(targetDirectory))
        {
            return Task.CompletedTask;
        }

        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        string backupFilePath = GetBackupFilePath(directoryPath);
        if (File.Exists(backupFilePath))
        {
            File.Copy(backupFilePath, lockFilePath, true);
        }
        return Task.CompletedTask;
    }
    private static readonly ThreadLocal<HashAlgorithm> HashAlgorithm = new(SHA256.Create);
    private static string GetBackupFilePath(string outputPath)
    {
        string hashedPath = Convert.ToHexString((HashAlgorithm.Value ?? throw new InvalidOperationException("unable to get hash algorithm")).ComputeHash(Encoding.UTF8.GetBytes(outputPath))).Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase);
        return Path.Combine(Path.GetTempPath(), Constants.TempDirectoryName, "backup", hashedPath, LockFileName);
    }
    public void DeleteLockFile(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        string lockFilePath = Path.Combine(directoryPath, LockFileName);
        if (File.Exists(lockFilePath))
        {
            File.Delete(lockFilePath);
        }
    }
}
