using AsyncKeyedLock;

namespace ReQuesty.Builder.WorkspaceManagement;

public class DescriptionStorageService
{
    public const string ReQuestyDirectorySegment = ".requesty";
    internal const string DescriptionsSubDirectoryRelativePath = $"{ReQuestyDirectorySegment}/documents";
    private readonly string TargetDirectory;
    public DescriptionStorageService(string targetDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetDirectory);
        TargetDirectory = targetDirectory;
    }
    private static readonly AsyncKeyedLocker<string> localFilesLock = new(o =>
    {
        o.PoolSize = 20;
        o.PoolInitialFill = 1;
    });
    private string GetDescriptionFilePath(string clientName, string extension) => Path.Combine(TargetDirectory, DescriptionsSubDirectoryRelativePath, clientName, $"openapi.{extension}");
    public async Task UpdateDescriptionAsync(string clientName, Stream description, string extension = "yml", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(extension);
        string descriptionFilePath = GetDescriptionFilePath(clientName, extension);
        using (await localFilesLock.LockAsync(descriptionFilePath, cancellationToken).ConfigureAwait(false))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(descriptionFilePath) ?? throw new InvalidOperationException("The target path is invalid"));
            using FileStream fs = new(descriptionFilePath, FileMode.Create);
            description.Seek(0, SeekOrigin.Begin);
            await description.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }
    }
    public async Task<Stream?> GetDescriptionAsync(string clientName, string extension = "yml", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentNullException.ThrowIfNull(extension);
        string descriptionFilePath = GetDescriptionFilePath(clientName, extension);
        if (!File.Exists(descriptionFilePath))
        {
            return null;
        }

        using (await localFilesLock.LockAsync(descriptionFilePath, cancellationToken).ConfigureAwait(false))
        {
            using FileStream fs = new(descriptionFilePath, FileMode.Open);
            MemoryStream ms = new();
            await fs.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }
    public void RemoveDescription(string clientName, string extension = "yml")
    {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentNullException.ThrowIfNull(extension);
        string descriptionFilePath = GetDescriptionFilePath(clientName, extension);
        if (File.Exists(descriptionFilePath))
        {
            File.Delete(descriptionFilePath);
        }
    }
    public void Clean()
    {
        string requestyDirectoryPath = Path.Combine(TargetDirectory, DescriptionsSubDirectoryRelativePath);
        if (Path.Exists(requestyDirectoryPath))
        {
            Directory.Delete(requestyDirectoryPath, true);
        }
    }
}
