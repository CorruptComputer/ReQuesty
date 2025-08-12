using ReQuesty.Builder.WorkspaceManagement;
using Xunit;

namespace ReQuesty.Builder.Tests.WorkspaceManagement;
public sealed class WorkspaceConfigurationStorageServiceTests : IDisposable
{
    [Fact]
    public async Task DefensiveProgrammingAsync()
    {
        Assert.Throws<ArgumentException>(() => new WorkspaceConfigurationStorageService(string.Empty));
        WorkspaceConfigurationStorageService service = new(Directory.GetCurrentDirectory());
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateWorkspaceConfigurationAsync(null!, null));
    }
    private readonly string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    [Fact]
    public async Task InitializesAsync()
    {
        WorkspaceConfigurationStorageService service = new(tempPath);
        await service.InitializeAsync();
        Assert.True(File.Exists(Path.Combine(tempPath, WorkspaceConfigurationStorageService.ReQuestyDirectorySegment, WorkspaceConfigurationStorageService.ConfigurationFileName)));
    }
    [Fact]
    public async Task FailsOnDoubleInitAsync()
    {
        WorkspaceConfigurationStorageService service = new(tempPath);
        await service.InitializeAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync());
    }
    [Fact]
    public async Task ReturnsNullOnNonInitializedAsync()
    {
        WorkspaceConfigurationStorageService service = new(tempPath);
        (WorkspaceConfiguration? config, Microsoft.OpenApi.ApiManifest.ApiManifestDocument? manifest) = await service.GetWorkspaceConfigurationAsync();
        Assert.Null(config);
        Assert.Null(manifest);
    }
    [Fact]
    public async Task ReturnsConfigurationWhenInitializedAsync()
    {
        WorkspaceConfigurationStorageService service = new(tempPath);
        await service.InitializeAsync();
        (WorkspaceConfiguration? result, Microsoft.OpenApi.ApiManifest.ApiManifestDocument? manifest) = await service.GetWorkspaceConfigurationAsync();
        Assert.NotNull(result);
        Assert.Null(manifest);
    }
    [Fact]
    public async Task ReturnsIsInitializedAsync()
    {
        WorkspaceConfigurationStorageService service = new(tempPath);
        await service.InitializeAsync();
        bool result = await service.IsInitializedAsync();
        Assert.True(result);
    }
    [Fact]
    public async Task DoesNotReturnIsInitializedAsync()
    {
        WorkspaceConfigurationStorageService service = new(tempPath);
        bool result = await service.IsInitializedAsync();
        Assert.False(result);
    }
    [Fact]
    public async Task BackupsAndRestoresAsync()
    {
        WorkspaceConfigurationStorageService service = new(tempPath);
        await service.InitializeAsync();
        await service.BackupConfigAsync();
        string targetConfigFile = Path.Combine(tempPath, WorkspaceConfigurationStorageService.ReQuestyDirectorySegment, WorkspaceConfigurationStorageService.ConfigurationFileName);
        File.Delete(targetConfigFile);
        Assert.False(File.Exists(targetConfigFile));
        await service.RestoreConfigAsync();
        Assert.True(File.Exists(targetConfigFile));
    }
    public void Dispose()
    {
        if (Directory.Exists(tempPath))
        {
            Directory.Delete(tempPath, true);
        }

        GC.SuppressFinalize(this);
    }
}
