using System.Text.Json.Nodes;
using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.OpenApiExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Moq;
using Xunit;

namespace ReQuesty.Builder.Tests.OpenApiExtensions;
public sealed class OpenApiAiAdaptiveCardExtensionTest : IDisposable
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
        {
            "data_path": "$.items",
            "file": "path_to_file",
            "title": "title",
            "url": "https://example.com"
        }
        """;
        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(oaiValueRepresentation));
        JsonNode? oaiValue = JsonNode.Parse(stream);
        OpenApiAiAdaptiveCardExtension value = OpenApiAiAdaptiveCardExtension.Parse(oaiValue!);
        Assert.NotNull(value);
        Assert.Equal("$.items", value.DataPath);
        Assert.Equal("path_to_file", value.File);
        Assert.Equal("title", value.Title);
        Assert.Equal("https://example.com", value.Url);
    }
    private readonly string TempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    [Fact]
    public async Task ParsesInDocumentAsync()
    {
        string documentContent = @"openapi: 3.0.0
info:
  title: Graph Users
  version: 0.0.0
servers:
  - url: https://graph.microsoft.com/v1.0
    description: The Microsoft Graph API
tags: []
paths:
  /users:
    get:
      operationId: getUsers
      parameters: []
      responses:
        '200':
          description: The request has succeeded.
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/User'
      x-ai-adaptive-card:
        data_path: $.users
        file: path_to_file
        title: title
        url: https://example.com
  /users/{id}:
    get:
      operationId: getUser
      parameters:
        - name: id
          in: path
          required: true
          description: The user id
          schema:
            type: string
      responses:
        '200':
          description: The request has succeeded.
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/User'
      x-ai-adaptive-card:
        data_path: $.user
        file: path_to_file
        title: title
        url: https://example.com
components:
  schemas:
    User:
      type: object
      required:
        - id
        - displayName
      properties:
        id:
          type: string
        displayName:
          type: string";
        Directory.CreateDirectory(TempDirectory);
        string documentPath = Path.Combine(TempDirectory, "document.yaml");
        await File.WriteAllTextAsync(documentPath, documentContent);
        Mock<ILogger<OpenApiAiAdaptiveCardExtension>> mockLogger = new();
        OpenApiDocumentDownloadService documentDownloadService = new(_httpClient, mockLogger.Object);
        GenerationConfiguration generationConfig = new() { OutputPath = TempDirectory, PluginTypes = [PluginType.APIPlugin] };
        (Stream openApiDocumentStream, bool _) = await documentDownloadService.LoadStreamAsync(documentPath, generationConfig);
        OpenApiDocument? document = await documentDownloadService.GetDocumentFromStreamAsync(openApiDocumentStream, generationConfig);
        Assert.NotNull(document);
        Assert.NotNull(document.Paths);
        Assert.NotNull(document.Paths["/users"].Operations!.FirstOrDefault().Value.Extensions);
        Assert.True(document.Paths["/users"].Operations!.FirstOrDefault().Value.Extensions!.TryGetValue(OpenApiAiAdaptiveCardExtension.Name, out IOpenApiExtension? adaptiveCardExtension));
        Assert.NotNull(adaptiveCardExtension);
    }

    [Fact]
    public void Serializes()
    {
        OpenApiAiAdaptiveCardExtension value = new()
        {
            DataPath = "$.items",
            File = "path_to_file",
            Title = "title",
            Url = "https://example.com"
        };
        using StringWriter sWriter = new();
        OpenApiJsonWriter writer = new(sWriter, new OpenApiJsonWriterSettings { Terse = true });


        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        string result = sWriter.ToString();
        Assert.Equal("{\"data_path\":\"$.items\",\"file\":\"path_to_file\",\"title\":\"title\",\"url\":\"https://example.com\"}", result);
    }
}
