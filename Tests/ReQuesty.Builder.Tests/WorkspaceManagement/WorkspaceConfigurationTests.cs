using ReQuesty.Builder.WorkspaceManagement;
using Xunit;

namespace ReQuesty.Builder.Tests.Manifest;
public sealed class WorkspaceConfigurationTests
{
    [Fact]
    public void Clones()
    {
        WorkspaceConfiguration source = new()
        {
            Clients = { { "GraphClient", new ApiClientConfiguration { ClientNamespaceName = "foo" } } },
        };
        WorkspaceConfiguration cloned = (WorkspaceConfiguration)source.Clone();
        Assert.NotNull(cloned);
        Assert.Equal(source.Clients.Count, cloned.Clients.Count);
    }
}
