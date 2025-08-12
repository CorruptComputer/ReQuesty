using ReQuesty.Builder.Lock;
using Xunit;

namespace ReQuesty.Builder.Tests.Lock;

public class LockManagementServiceTests
{
    [Fact]
    public async Task DefensiveProgrammingAsync()
    {
        LockManagementService lockManagementService = new();
        Assert.Throws<ArgumentNullException>(() => lockManagementService.GetDirectoriesContainingLockFile(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.GetLockFromDirectoryAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.GetLockFromStreamAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.WriteLockFileAsync(null!, new ReQuestyLock()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => lockManagementService.WriteLockFileAsync("path", null!));
    }
    [Fact]
    public async Task IdentityAsync()
    {
        LockManagementService lockManagementService = new();
        string descriptionPath = Path.Combine(Path.GetTempPath(), "description.yml");
        ReQuestyLock lockFile = new()
        {
            ClientClassName = "foo",
            ClientNamespaceName = "bar",
            DescriptionLocation = descriptionPath,
        };
        string path = Path.GetTempPath();
        await lockManagementService.WriteLockFileAsync(path, lockFile);
        lockFile.DescriptionLocation = Path.GetFullPath(descriptionPath); // expected since we write the relative path but read to the full path
        ReQuestyLock? result = await lockManagementService.GetLockFromDirectoryAsync(path);
        Assert.Equal(lockFile, result, new ReQuestyLockComparer());
    }
    [Fact]
    public async Task UsesRelativePathsAsync()
    {
        string tmpPath = Path.Combine(Path.GetTempPath(), "tests", "requesty");
        LockManagementService lockManagementService = new();
        string descriptionPath = Path.Combine(tmpPath, "information", "description.yml");
        string? descriptionDirectory = Path.GetDirectoryName(descriptionPath);
        Directory.CreateDirectory(descriptionDirectory!);
        ReQuestyLock lockFile = new()
        {
            DescriptionLocation = descriptionPath,
        };
        string outputDirectory = Path.Combine(tmpPath, "output");
        Directory.CreateDirectory(outputDirectory);
        await lockManagementService.WriteLockFileAsync(outputDirectory, lockFile);
        Assert.Equal("../information/description.yml", lockFile.DescriptionLocation, StringComparer.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task DeletesALockAsync()
    {
        LockManagementService lockManagementService = new();
        string descriptionPath = Path.Combine(Path.GetTempPath(), "description.yml");
        ReQuestyLock lockFile = new()
        {
            ClientClassName = "foo",
            ClientNamespaceName = "bar",
            DescriptionLocation = descriptionPath,
        };
        string path = Path.GetTempPath();
        await lockManagementService.WriteLockFileAsync(path, lockFile);
        lockManagementService.DeleteLockFile(path);
        Assert.Null(await lockManagementService.GetLockFromDirectoryAsync(path));
    }
}
