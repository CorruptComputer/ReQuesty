namespace ReQuesty.Builder.Lock;
/// <summary>
/// A service that manages the lock file for a ReQuesty project.
/// </summary>
public interface ILockManagementService
{
    /// <summary>
    /// Gets the lock file for a ReQuesty project by crawling the directory tree.
    /// </summary>
    /// <param name="searchDirectory">The root directory to crawl</param>
    IEnumerable<string> GetDirectoriesContainingLockFile(string searchDirectory);
    /// <summary>
    /// Gets the lock file for a ReQuesty project by reading it from the target directory.
    /// </summary>
    /// <param name="directoryPath">The target directory to read the lock file from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<ReQuestyLock?> GetLockFromDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets the lock file for a ReQuesty project by reading it from a stream.
    /// </summary>
    /// <param name="stream">The stream to read the lock file from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<ReQuestyLock?> GetLockFromStreamAsync(Stream stream, CancellationToken cancellationToken = default);
    /// <summary>
    /// Writes the lock file for a ReQuesty project to the target directory.
    /// </summary>
    /// <param name="directoryPath">The target directory to write the lock file to.</param>
    /// <param name="lockInfo">The lock information to write.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task WriteLockFileAsync(string directoryPath, ReQuestyLock lockInfo, CancellationToken cancellationToken = default);
    /// <summary>
    /// Backs up the lock file for a ReQuesty project to the target directory.
    /// </summary>
    /// <param name="directoryPath">The target directory to write the lock file from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task BackupLockFileAsync(string directoryPath, CancellationToken cancellationToken = default);
    /// <summary>
    /// Restores the lock file for a ReQuesty project to the target directory.
    /// </summary>
    /// <param name="directoryPath">The target directory to write the lock file to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task RestoreLockFileAsync(string directoryPath, CancellationToken cancellationToken = default);
}
