using System.Text;
using System.Text.Json.Nodes;
using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.OpenApiExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Moq;
using Xunit;

namespace ReQuesty.Builder.Tests.OpenApiExtensions;
public sealed class OpenApiAiReasoningInstructionsExtensionTests : IDisposable
{
    private readonly HttpClient _httpClient = new();
    public void Dispose()
    {
        _httpClient.Dispose();
    }
    [Fact]
    public void Parses()
    {
        string oaiValueRepresentation =
        """
        [
            "This is a description",
            "This is a description 2"
        ]
        """;
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(oaiValueRepresentation));
        JsonNode? oaiValue = JsonNode.Parse(stream);
        OpenApiAiReasoningInstructionsExtension value = OpenApiAiReasoningInstructionsExtension.Parse(oaiValue!);
        Assert.NotNull(value);
        Assert.Equal("This is a description", value.ReasoningInstructions[0]);
    }
    private readonly string TempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    [Fact]
    public async Task ParsesInDocumentAsync()
    {
        string documentContent = @"openapi: 3.0.0
info:
  title: Test
  version: 1.0.0
  x-ai-reasoning-instructions:
    - This is a description
    - This is a description 2";
        Directory.CreateDirectory(TempDirectory);
        string documentPath = Path.Combine(TempDirectory, "document.yaml");
        await File.WriteAllTextAsync(documentPath, documentContent);
        Mock<ILogger<OpenApiAiReasoningInstructionsExtension>> mockLogger = new();
        OpenApiDocumentDownloadService documentDownloadService = new(_httpClient, mockLogger.Object);
        GenerationConfiguration generationConfig = new() { OutputPath = TempDirectory, PluginTypes = [PluginType.APIPlugin] };
        (Stream openApiDocumentStream, bool _) = await documentDownloadService.LoadStreamAsync(documentPath, generationConfig);
        OpenApiDocument? document = await documentDownloadService.GetDocumentFromStreamAsync(openApiDocumentStream, generationConfig);
        Assert.NotNull(document);
        Assert.NotNull(document.Info);
        Assert.True(document.Info.Extensions!.TryGetValue(OpenApiAiReasoningInstructionsExtension.Name, out IOpenApiExtension? descriptionExtension));
        Assert.IsType<OpenApiAiReasoningInstructionsExtension>(descriptionExtension);
        Assert.Equal("This is a description", ((OpenApiAiReasoningInstructionsExtension)descriptionExtension).ReasoningInstructions[0]);
    }
    [Fact]
    public void Serializes()
    {
        OpenApiAiReasoningInstructionsExtension value = new()
        {
            ReasoningInstructions = [
                "This is a description",
                "This is a description 2",
            ]
        };
        using StringWriter sWriter = new();
        OpenApiJsonWriter writer = new(sWriter, new OpenApiJsonWriterSettings { Terse = true });


        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        string result = sWriter.ToString();
        Assert.Equal("[\"This is a description\",\"This is a description 2\"]", result);
    }
}
