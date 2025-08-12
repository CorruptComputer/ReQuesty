using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.OpenApiExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Moq;
using Xunit;

namespace ReQuesty.Builder.Tests.OpenApiExtensions;
public sealed class OpenApiDescriptionForModelExtensionTests : IDisposable
{
    private readonly HttpClient _httpClient = new();
    public void Dispose()
    {
        _httpClient.Dispose();
    }
    [Fact]
    public void Parses()
    {
        OpenApiDescriptionForModelExtension value = OpenApiDescriptionForModelExtension.Parse("This is a description");
        Assert.NotNull(value);
        Assert.Equal("This is a description", value.Description);
    }
    private readonly string TempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    [Fact]
    public async Task ParsesInDocumentAsync()
    {
        string documentContent = @"openapi: 3.0.0
info:
  title: Test
  version: 1.0.0
  x-ai-description: This is a description";
        Directory.CreateDirectory(TempDirectory);
        string documentPath = Path.Combine(TempDirectory, "document.yaml");
        await File.WriteAllTextAsync(documentPath, documentContent);
        Mock<ILogger<OpenApiDescriptionForModelExtension>> mockLogger = new();
        OpenApiDocumentDownloadService documentDownloadService = new(_httpClient, mockLogger.Object);
        GenerationConfiguration generationConfig = new() { OutputPath = TempDirectory, PluginTypes = [PluginType.APIPlugin] };
        (Stream openApiDocumentStream, bool _) = await documentDownloadService.LoadStreamAsync(documentPath, generationConfig);
        OpenApiDocument? document = await documentDownloadService.GetDocumentFromStreamAsync(openApiDocumentStream, generationConfig);
        Assert.NotNull(document);
        Assert.NotNull(document.Info);
        Assert.True(document.Info.Extensions!.TryGetValue(OpenApiDescriptionForModelExtension.Name, out IOpenApiExtension? descriptionExtension));
        Assert.IsType<OpenApiDescriptionForModelExtension>(descriptionExtension);
        Assert.Equal("This is a description", ((OpenApiDescriptionForModelExtension)descriptionExtension).Description);
    }
    [Fact]
    public void Serializes()
    {
        OpenApiDescriptionForModelExtension value = new()
        {
            Description = "This is a description",
        };
        using StringWriter sWriter = new();
        OpenApiJsonWriter writer = new(sWriter, new OpenApiJsonWriterSettings { Terse = true });


        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        string result = sWriter.ToString();
        Assert.Equal("\"This is a description\"", result);
    }
}
