using ReQuesty.Builder.Configuration;
using Xunit;

namespace ReQuesty.Builder.Tests.Configuration;
public class GenerationConfigurationTests
{
    [Fact]
    public void Clones()
    {
        GenerationConfiguration generationConfiguration = new()
        {
            ClientClassName = "class1",
            IncludePatterns = null!,
        };
        GenerationConfiguration? clone = generationConfiguration.Clone() as GenerationConfiguration;
        Assert.NotNull(clone);
        Assert.Equal(generationConfiguration.ClientClassName, clone.ClientClassName);
        Assert.NotNull(clone.IncludePatterns);
        Assert.Empty(clone.IncludePatterns);
        clone.ClientClassName = "class2";
        Assert.NotEqual(generationConfiguration.ClientClassName, clone.ClientClassName);
    }
    [Fact]
    public void ToApiDependency()
    {
        GenerationConfiguration generationConfiguration = new()
        {
            ClientClassName = "class1",
            IncludePatterns = null!,
            OpenAPIFilePath = "https://pet.store/openapi.yaml",
            ApiRootUrl = "https://pet.store/api",
        };
        Microsoft.OpenApi.ApiManifest.ApiDependency apiDependency = generationConfiguration.ToApiDependency("foo", new Dictionary<string, HashSet<string>>{
            { "foo/bar", new HashSet<string>{"GET"}}
        }, Path.GetTempPath());
        Assert.NotNull(apiDependency);
        Assert.NotNull(apiDependency.Extensions);
        Assert.Equal("foo", apiDependency.Extensions[GenerationConfiguration.ReQuestyHashManifestExtensionKey]!.GetValue<string>());
        Assert.NotEmpty(apiDependency.Requests);
        Assert.Equal("foo/bar", apiDependency.Requests[0].UriTemplate);
        Assert.Equal("GET", apiDependency.Requests[0].Method);
    }
    [Fact]
    public void ToApiDependencyDoesNotIncludeConfigHashIfEmpty()
    {
        GenerationConfiguration generationConfiguration = new()
        {
            ClientClassName = "class1",
            IncludePatterns = null!,
            OpenAPIFilePath = "https://pet.store/openapi.yaml",
            ApiRootUrl = "https://pet.store/api",
        };
        Microsoft.OpenApi.ApiManifest.ApiDependency apiDependency = generationConfiguration.ToApiDependency(string.Empty, new Dictionary<string, HashSet<string>>{
            { "foo/bar", new HashSet<string>{"GET"}}
        }, Path.GetTempPath());
        Assert.NotNull(apiDependency);
        Assert.NotNull(apiDependency.Extensions);
        Assert.False(apiDependency.Extensions.ContainsKey(GenerationConfiguration.ReQuestyHashManifestExtensionKey));
        Assert.NotEmpty(apiDependency.Requests);
        Assert.Equal("foo/bar", apiDependency.Requests[0].UriTemplate);
        Assert.Equal("GET", apiDependency.Requests[0].Method);
    }
}
