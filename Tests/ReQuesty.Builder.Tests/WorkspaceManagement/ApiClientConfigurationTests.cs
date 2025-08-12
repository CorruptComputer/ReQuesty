using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.WorkspaceManagement;
using Microsoft.OpenApi.ApiManifest;
using Xunit;

namespace ReQuesty.Builder.Tests.WorkspaceManagement;

public sealed class ApiClientConfigurationTests
{
    [Fact]
    public void Clones()
    {
        ApiClientConfiguration clientConfig = new()
        {
            ClientNamespaceName = "foo",
            DescriptionLocation = "bar",
            ExcludeBackwardCompatible = true,
            ExcludePatterns = [
                "exclude"
            ],
            IncludeAdditionalData = true,
            IncludePatterns = [
                "include"
            ],
            Language = "csharp",
            OutputPath = "output",
            StructuredMimeTypes = [
                "mime"
            ],
            UsesBackingStore = true,
        };
        ApiClientConfiguration cloned = (ApiClientConfiguration)clientConfig.Clone();
        Assert.NotNull(cloned);
        Assert.Equal(clientConfig.ClientNamespaceName, cloned.ClientNamespaceName);
        Assert.Equal(clientConfig.DescriptionLocation, cloned.DescriptionLocation);
        Assert.Equal(clientConfig.ExcludeBackwardCompatible, cloned.ExcludeBackwardCompatible);
        Assert.Equal(clientConfig.ExcludePatterns, cloned.ExcludePatterns);
        Assert.Equal(clientConfig.IncludeAdditionalData, cloned.IncludeAdditionalData);
        Assert.Equal(clientConfig.IncludePatterns, cloned.IncludePatterns);
        Assert.Equal(clientConfig.Language, cloned.Language);
        Assert.Equal(clientConfig.OutputPath, cloned.OutputPath);
        Assert.Equal(clientConfig.StructuredMimeTypes, cloned.StructuredMimeTypes);
        Assert.Equal(clientConfig.UsesBackingStore, cloned.UsesBackingStore);
    }

    [Fact]
    public void CreatesApiClientConfigurationFromGenerationConfiguration()
    {
        GenerationConfiguration generationConfiguration = new()
        {
            ApiManifestPath = "manifest",
            ClientClassName = "client",
            ClientNamespaceName = "namespace",
            ExcludeBackwardCompatible = true,
            ExcludePatterns = ["exclude"],
            IncludeAdditionalData = true,
            IncludePatterns = ["include"],
            Language = GenerationLanguage.CSharp,
            OpenAPIFilePath = "openapi",
            OutputPath = "output",
            UsesBackingStore = true,
            StructuredMimeTypes = ["application/json"],
        };
        ApiClientConfiguration clientConfig = new(generationConfiguration);
        Assert.NotNull(clientConfig);
        Assert.Equal(generationConfiguration.ClientNamespaceName, clientConfig.ClientNamespaceName);
        Assert.Equal(generationConfiguration.OpenAPIFilePath, clientConfig.DescriptionLocation);
        Assert.Equal(generationConfiguration.ExcludeBackwardCompatible, clientConfig.ExcludeBackwardCompatible);
        Assert.Equal(generationConfiguration.ExcludePatterns, clientConfig.ExcludePatterns);
        Assert.Equal(generationConfiguration.IncludeAdditionalData, clientConfig.IncludeAdditionalData);
        Assert.Equal(generationConfiguration.IncludePatterns, clientConfig.IncludePatterns);
        Assert.Equal(generationConfiguration.Language.ToString(), clientConfig.Language, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(generationConfiguration.OutputPath, clientConfig.OutputPath);
        Assert.Equal(generationConfiguration.StructuredMimeTypes, clientConfig.StructuredMimeTypes);
        Assert.Equal(generationConfiguration.UsesBackingStore, clientConfig.UsesBackingStore);
    }
    [Fact]
    public void UpdatesGenerationConfigurationFromApiClientConfiguration()
    {
        ApiClientConfiguration clientConfiguration = new()
        {
            ClientNamespaceName = "namespace",
            DescriptionLocation = "openapi",
            ExcludeBackwardCompatible = true,
            ExcludePatterns = ["exclude"],
            IncludeAdditionalData = true,
            IncludePatterns = ["include"],
            Language = "csharp",
            OutputPath = "output",
            StructuredMimeTypes = ["application/json"],
            UsesBackingStore = true,
        };
        GenerationConfiguration generationConfiguration = new();
        clientConfiguration.UpdateGenerationConfigurationFromApiClientConfiguration(generationConfiguration, "client", [
            new RequestInfo
            {
                Method = "GET",
                UriTemplate = "path/bar",
            },
            new RequestInfo
            {
                Method = "PATH",
                UriTemplate = "path/baz",
            },
        ]);
        Assert.Equal(clientConfiguration.ClientNamespaceName, generationConfiguration.ClientNamespaceName);
        Assert.Equal(GenerationLanguage.CSharp, generationConfiguration.Language);
        Assert.Equal(clientConfiguration.DescriptionLocation, generationConfiguration.OpenAPIFilePath);
        Assert.Equal(clientConfiguration.ExcludeBackwardCompatible, generationConfiguration.ExcludeBackwardCompatible);
        Assert.Equal(clientConfiguration.ExcludePatterns, generationConfiguration.ExcludePatterns);
        Assert.Equal(clientConfiguration.IncludeAdditionalData, generationConfiguration.IncludeAdditionalData);
        Assert.Equal(clientConfiguration.IncludePatterns, generationConfiguration.IncludePatterns);
        Assert.Equal(clientConfiguration.OutputPath, generationConfiguration.OutputPath);
        Assert.Equal(clientConfiguration.StructuredMimeTypes, generationConfiguration.StructuredMimeTypes);
        Assert.Equal(clientConfiguration.UsesBackingStore, generationConfiguration.UsesBackingStore);
        Assert.Empty(generationConfiguration.Serializers);
        Assert.Empty(generationConfiguration.Deserializers);
        Assert.Equal(2, generationConfiguration.PatternsOverride.Count);
    }

}
