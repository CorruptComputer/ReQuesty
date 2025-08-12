using ReQuesty.Builder.WorkspaceManagement;
using Xunit;

namespace ReQuesty.Builder.Tests.WorkspaceManagement;

public sealed class DescriptionStorageServiceTests
{
    private readonly string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    [Fact]
    public async Task StoresADescriptionAsync()
    {
        DescriptionStorageService service = new(tempPath);
        using MemoryStream stream = new();
        stream.WriteByte(0x1);
        await service.UpdateDescriptionAsync("clientName", stream);
        using Stream? result = await service.GetDescriptionAsync("clientName");
        Assert.NotNull(result);
    }
    [Fact]
    public async Task DeletesAStoredDescriptionAsync()
    {
        DescriptionStorageService service = new(tempPath);
        using MemoryStream stream = new();
        stream.WriteByte(0x1);
        await service.UpdateDescriptionAsync("clientNameA", stream);
        service.RemoveDescription("clientNameA");
        Stream? result = await service.GetDescriptionAsync("clientNameA");
        Assert.Null(result);
    }
    [Fact]
    public async Task ReturnsNothingIfNoDescriptionIsPresentAsync()
    {
        DescriptionStorageService service = new(tempPath);
        Stream? result = await service.GetDescriptionAsync("clientNameB");
        Assert.Null(result);
    }
    [Fact]
    public async Task DefensiveAsync()
    {
        Assert.Throws<ArgumentException>(() => new DescriptionStorageService(string.Empty));
        DescriptionStorageService service = new(tempPath);
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateDescriptionAsync(null!, Stream.Null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateDescriptionAsync("foo", null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetDescriptionAsync(null!));
    }
}
