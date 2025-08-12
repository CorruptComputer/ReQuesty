using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Lock;
using ReQuesty.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ReQuesty.Builder.Tests.WorkspaceManagement;

public sealed class WorkspaceManagementServiceTests : IDisposable
{
    private readonly string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly HttpClient httpClient = new();
    [Fact]
    public void Defensive()
    {
        Assert.Throws<ArgumentNullException>(() => new WorkspaceManagementService(null!, httpClient));
        Assert.Throws<ArgumentNullException>(() => new WorkspaceManagementService(Mock.Of<ILogger>(), null!));
    }
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task IsClientPresentReturnsFalseOnNoClientAsync(bool usesConfig)
    {
        ILogger mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        WorkspaceManagementService service = new(mockLogger, httpClient, usesConfig, tempPath);
        bool result = await service.IsConsumerPresentAsync("clientName");
        Assert.False(result);
    }
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [Theory]
    public async Task ShouldGenerateReturnsTrueAsync(bool usesConfig, bool cleanOutput)
    {
        ILogger mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        WorkspaceManagementService service = new(mockLogger, httpClient, usesConfig, tempPath);
        GenerationConfiguration configuration = new()
        {
            ClientClassName = "clientName",
            OutputPath = tempPath,
            OpenAPIFilePath = Path.Combine(tempPath, "openapi.yaml"),
            CleanOutput = cleanOutput,
        };
        bool result = await service.ShouldGenerateAsync(configuration, "foo");
        Assert.True(result);
    }
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task ShouldGenerateReturnsFalseAsync(bool usesConfig)
    {
        ILogger mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        WorkspaceManagementService service = new(mockLogger, httpClient, usesConfig, tempPath);
        GenerationConfiguration configuration = new()
        {
            ClientClassName = "clientName",
            OutputPath = tempPath,
            OpenAPIFilePath = Path.Combine(tempPath, "openapi.yaml"),
            ApiRootUrl = "https://graph.microsoft.com",
        };
        Directory.CreateDirectory(tempPath);
        await service.UpdateStateFromConfigurationAsync(
            configuration,
            "foo",
            new Dictionary<string, HashSet<string>> {
                { "/foo", new HashSet<string> { "GET" } }
            },
            Stream.Null);
        bool result = await service.ShouldGenerateAsync(configuration, "foo");
        Assert.False(result);
    }
    [Fact]
    public async Task RemovesAClientAsync()
    {
        ILogger mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        WorkspaceManagementService service = new(mockLogger, httpClient, true, tempPath);
        GenerationConfiguration configuration = new()
        {
            ClientClassName = "clientName",
            OutputPath = tempPath,
            OpenAPIFilePath = Path.Combine(tempPath, "openapi.yaml"),
            ApiRootUrl = "https://graph.microsoft.com",
        };
        Directory.CreateDirectory(tempPath);
        await service.UpdateStateFromConfigurationAsync(
            configuration,
            "foo",
            new Dictionary<string, HashSet<string>> {
                { "/foo", new HashSet<string> { "GET" } }
            },
            Stream.Null);
        await service.RemoveClientAsync("clientName");
        bool result = await service.IsConsumerPresentAsync("clientName");
        Assert.False(result);
    }
    [Fact]
    public async Task RemovesAPluginAsync()
    {
        ILogger mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        WorkspaceManagementService service = new(mockLogger, httpClient, true, tempPath);
        GenerationConfiguration configuration = new()
        {
            ClientClassName = "clientName",
            OutputPath = tempPath,
            OpenAPIFilePath = Path.Combine(tempPath, "openapi.yaml"),
            ApiRootUrl = "https://graph.microsoft.com",
            PluginTypes = [PluginType.APIManifest],
        };
        Directory.CreateDirectory(tempPath);
        await service.UpdateStateFromConfigurationAsync(
            configuration,
            "foo",
            new Dictionary<string, HashSet<string>> {
                { "/foo", new HashSet<string> { "GET" } }
            },
            Stream.Null);
        await service.RemovePluginAsync("clientName");
        bool result = await service.IsConsumerPresentAsync("clientName");
        Assert.False(result);
    }
    [Fact]
    public async Task FailsOnMigrateWithoutReQuestyConfigModeAsync()
    {
        ILogger mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        WorkspaceManagementService service = new(mockLogger, httpClient, false, tempPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateFromLockFileAsync(string.Empty, tempPath));
    }
    [Fact]
    public async Task FailsWhenTargetLockDirectoryIsNotSubDirectoryAsync()
    {
        ILogger mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        WorkspaceManagementService service = new(mockLogger, httpClient, true, tempPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateFromLockFileAsync(string.Empty, Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())));
    }
    [Fact]
    public async Task FailsWhenNoLockFilesAreFoundAsync()
    {
        ILogger mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        WorkspaceManagementService service = new(mockLogger, httpClient, true, tempPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateFromLockFileAsync(string.Empty, tempPath));
    }
    [Fact]
    public async Task FailsOnMultipleLockFilesAndClientNameAsync()
    {
        ILogger mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        WorkspaceManagementService service = new(mockLogger, httpClient, true, tempPath);
        Directory.CreateDirectory(Path.Combine(tempPath, "client1"));
        Directory.CreateDirectory(Path.Combine(tempPath, "client2"));
        File.WriteAllText(Path.Combine(tempPath, "client1", LockManagementService.LockFileName), "foo");
        File.WriteAllText(Path.Combine(tempPath, "client2", LockManagementService.LockFileName), "foo");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateFromLockFileAsync("bar", tempPath));
    }
    [Fact]
    public async Task MigratesAClientAsync()
    {
        ILogger mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        WorkspaceManagementService service = new(mockLogger, httpClient, true, tempPath);
        string descriptionPath = Path.Combine(tempPath, "description.yml");
        GenerationConfiguration generationConfiguration = new()
        {
            ClientClassName = "clientName",
            OutputPath = Path.Combine(tempPath, "client"),
            OpenAPIFilePath = descriptionPath,
            ApiRootUrl = "https://graph.microsoft.com",
        };
        Directory.CreateDirectory(generationConfiguration.OutputPath);
        await File.WriteAllTextAsync(descriptionPath, @$"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://localhost:443
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: object
                properties:
                  bar:
                    type: object
                    properties:
                      foo:
                        type: string");
        WorkspaceManagementService classicService = new(mockLogger, httpClient, false, tempPath);
        await classicService.UpdateStateFromConfigurationAsync(
            generationConfiguration,
            "foo",
            new Dictionary<string, HashSet<string>> {
                { "/foo", new HashSet<string> { "GET" } }
            },
            Stream.Null);
        IEnumerable<string> clientNames = await service.MigrateFromLockFileAsync("clientName", tempPath);
        Assert.Single(clientNames);
        Assert.Equal("clientName", clientNames.First());
        Assert.False(File.Exists(Path.Combine(tempPath, LockManagementService.LockFileName)));
        Assert.True(File.Exists(Path.Combine(tempPath, WorkspaceConfigurationStorageService.ReQuestyDirectorySegment, WorkspaceConfigurationStorageService.ConfigurationFileName)));
        Assert.True(File.Exists(Path.Combine(tempPath, WorkspaceConfigurationStorageService.ReQuestyDirectorySegment, WorkspaceConfigurationStorageService.ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(tempPath, DescriptionStorageService.DescriptionsSubDirectoryRelativePath, "clientName", "openapi.yml")));
    }
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [Theory]
    public async Task GetsADescriptionAsync(bool usesConfig, bool cleanOutput)
    {
        ILogger mockLogger = Mock.Of<ILogger>();
        Directory.CreateDirectory(tempPath);
        WorkspaceManagementService service = new(mockLogger, httpClient, usesConfig, tempPath);
        string descriptionPath = Path.Combine(tempPath, $"{DescriptionStorageService.DescriptionsSubDirectoryRelativePath}/clientName/openapi.yml");
        string outputPath = Path.Combine(tempPath, "client");
        Directory.CreateDirectory(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(descriptionPath)!);
        await File.WriteAllTextAsync(descriptionPath, @$"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://localhost:443
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: object
                properties:
                  bar:
                    type: object
                    properties:
                      foo:
                        type: string");
        Stream? descriptionCopy = await service.GetDescriptionCopyAsync("clientName", descriptionPath, cleanOutput);
        if (!usesConfig || cleanOutput)
        {
            Assert.Null(descriptionCopy);
        }
        else
        {
            Assert.NotNull(descriptionCopy);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(tempPath))
        {
            Directory.Delete(tempPath, true);
        }

        httpClient.Dispose();
    }
}
