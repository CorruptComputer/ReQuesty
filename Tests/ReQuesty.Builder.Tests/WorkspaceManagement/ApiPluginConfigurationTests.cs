using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.WorkspaceManagement;
using Xunit;

namespace ReQuesty.Builder.Tests.WorkspaceManagement;
public sealed class ApiPluginConfigurationTests
{
    [Fact]
    public void Defensive()
    {
        Assert.Throws<ArgumentNullException>(() => new ApiPluginConfiguration(null!));
    }
    [Fact]
    public void CopiesPluginTypesFromConfiguration()
    {
        GenerationConfiguration generationConfig = new()
        {
            PluginTypes = [PluginType.APIManifest]
        };
        ApiPluginConfiguration apiPluginConfig = new(generationConfig);
        Assert.NotNull(apiPluginConfig);
        Assert.Contains("APIManifest", apiPluginConfig.Types);
    }
    [Fact]
    public void Clones()
    {
        ApiPluginConfiguration apiPluginConfig = new()
        {
            Types = ["APIManifest"]
        };
        ApiPluginConfiguration cloned = (ApiPluginConfiguration)apiPluginConfig.Clone();
        Assert.NotNull(cloned);
        Assert.Equal(apiPluginConfig.Types, cloned.Types);
    }
    [Fact]
    public void UpdateGenerationConfigurationFromPluginConfiguration()
    {
        GenerationConfiguration generationConfig = new();
        ApiPluginConfiguration apiPluginConfig = new()
        {
            Types = ["APIManifest"]
        };
        apiPluginConfig.UpdateGenerationConfigurationFromApiPluginConfiguration(generationConfig, "Foo");
        Assert.Contains(PluginType.APIManifest, generationConfig.PluginTypes);
    }
}
