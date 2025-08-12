using System.Text.Json.Nodes;
using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.OpenApiExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Moq;
using Xunit;

namespace ReQuesty.Builder.Tests.OpenApiExtensions;
public sealed class OpenApiAiCapabilitiesExtensionTest : IDisposable
{
    private readonly HttpClient _httpClient = new();
    private readonly string TempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public void Dispose()
    {
        _httpClient.Dispose();
        if (Directory.Exists(TempDirectory))
        {
            Directory.Delete(TempDirectory, true);
        }
    }

    [Fact]
    public void Parses()
    {
        string oaiValueRepresentation =
        """
        {
            "response_semantics": {
                "data_path": "$.items",
                "static_template": {
                    "title": "Search for items",
                    "body": "Here are the items I found for you."
                },
                "properties": {
                    "title": "Some title",
                    "subtitle": "Some subtitle",
                    "url": "https://example.com",
                    "thumbnail_url": "https://example.com/thumbnail.jpg",
                    "information_protection_label": "confidential"
                },
                "oauth_card_path": "oauthCard.json"
            },
            "confirmation": {
                "type": "modal",
                "title": "Confirm action",
                "body": "Do you want to proceed?"
            },
            "security_info": {
                "data_handling": ["some data handling"]
            }
        }
        """;
        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(oaiValueRepresentation));
        JsonNode? oaiValue = JsonNode.Parse(stream);
        OpenApiAiCapabilitiesExtension value = OpenApiAiCapabilitiesExtension.Parse(oaiValue!);

        Assert.NotNull(value);

        ExtensionResponseSemantics? responseSemantics = value.ResponseSemantics;
        ExtensionConfirmation? confirmation = value.Confirmation;
        ExtensionSecurityInfo? securityInfo = value.SecurityInfo;
        Assert.NotNull(responseSemantics);
        Assert.NotNull(confirmation);
        Assert.NotNull(securityInfo);

        Assert.Equal("$.items", responseSemantics.DataPath);
        Assert.Equal("oauthCard.json", responseSemantics.OauthCardPath);
        JsonObject? staticTemplate = responseSemantics.StaticTemplate as JsonObject;
        Assert.NotNull(staticTemplate);
        Assert.Equal("Search for items", staticTemplate["title"]?.ToString());
        Assert.Equal("Here are the items I found for you.", staticTemplate["body"]?.ToString());

        ExtensionResponseSemanticsProperties? properties = responseSemantics.Properties;
        Assert.NotNull(properties);
        Assert.Equal("Some title", properties.Title);
        Assert.Equal("Some subtitle", properties.Subtitle);
        Assert.Equal("https://example.com", properties.Url);
        Assert.Equal("https://example.com/thumbnail.jpg", properties.ThumbnailUrl);
        Assert.Equal("confidential", properties.InformationProtectionLabel);

        Assert.Equal("modal", confirmation.Type);
        Assert.Equal("Confirm action", confirmation.Title);
        Assert.Equal("Do you want to proceed?", confirmation.Body);

        Assert.Equal("some data handling", securityInfo.DataHandling[0]);
    }

    [Fact]
    public async Task ParsesInDocumentAsync()
    {
        string documentContent = @"openapi: 3.0.0
info:
  title: Test API
  version: 0.0.0
servers:
  - url: https://api.example.com/v1
    description: Example API
paths:
  /items:
    get:
      operationId: getItems
      parameters: []
      responses:
        '200':
          description: The request has succeeded.
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Item'
      x-ai-capabilities:
        response_semantics:
          data_path: $.items
          static_template:
            title: Search for items
            body: Here are the items I found for you.
          properties:
            title: Some title
            subtitle: Some subtitle
            url: https://example.com
            thumbnail_url: https://example.com/thumbnail.jpg
            information_protection_label: confidential
          oauth_card_path: oauthCard.json
        confirmation:
          type: modal
          title: Confirm action
          body: Do you want to proceed?
        security_info:
          data_handling:
            - some data handling
components:
  schemas:
    Item:
      type: object
      properties:
        id:
          type: string
        name:
          type: string";

        Directory.CreateDirectory(TempDirectory);
        string documentPath = Path.Combine(TempDirectory, "document.yaml");
        await File.WriteAllTextAsync(documentPath, documentContent);
        Mock<ILogger<OpenApiAiCapabilitiesExtension>> mockLogger = new();
        OpenApiDocumentDownloadService documentDownloadService = new(_httpClient, mockLogger.Object);
        GenerationConfiguration generationConfig = new() { OutputPath = TempDirectory, PluginTypes = [PluginType.APIPlugin] };
        (Stream openApiDocumentStream, bool _) = await documentDownloadService.LoadStreamAsync(documentPath, generationConfig);
        OpenApiDocument? document = await documentDownloadService.GetDocumentFromStreamAsync(openApiDocumentStream, generationConfig);

        Assert.NotNull(document);
        Assert.NotNull(document.Paths);
        Assert.NotNull(document.Paths["/items"].Operations!.FirstOrDefault().Value.Extensions);
        Assert.True(document.Paths["/items"].Operations!.FirstOrDefault().Value.Extensions!.TryGetValue(OpenApiAiCapabilitiesExtension.Name, out IOpenApiExtension? capabilitiesExtension));
        Assert.NotNull(capabilitiesExtension);
    }

    [Fact]
    public void Serializes()
    {
        OpenApiAiCapabilitiesExtension value = new()
        {
            ResponseSemantics = new ExtensionResponseSemantics
            {
                DataPath = "$.items",
                StaticTemplate = new JsonObject
                {
                    ["title"] = "Search for items",
                    ["body"] = "Here are the items I found for you."
                },
                Properties = new ExtensionResponseSemanticsProperties
                {
                    Title = "Some title",
                    Subtitle = "Some subtitle",
                    Url = "https://example.com",
                    ThumbnailUrl = "https://example.com/thumbnail.jpg",
                    InformationProtectionLabel = "confidential"
                },
                OauthCardPath = "oauthCard.json"
            },
            Confirmation = new ExtensionConfirmation
            {
                Type = "modal",
                Title = "Confirm action",
                Body = "Do you want to proceed?"
            },
            SecurityInfo = new ExtensionSecurityInfo
            {
                DataHandling = ["some data handling"]
            }
        };
        using StringWriter sWriter = new();
        OpenApiJsonWriter writer = new(sWriter, new OpenApiJsonWriterSettings { Terse = true });


        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        string result = sWriter.ToString();

        Assert.Contains("\"response_semantics\":", result);
        Assert.Contains("data_path", result);
        Assert.Contains("$.items", result);
        Assert.Contains("static_template", result);
        Assert.Contains("title", result);
        Assert.Contains("Search for items", result);
        Assert.Contains("body", result);
        Assert.Contains("Here are the items I found for you", result);
        Assert.Contains("properties", result);
        Assert.Contains("title", result);
        Assert.Contains("Some title", result);
        Assert.Contains("subtitle", result);
        Assert.Contains("Some subtitle", result);
        Assert.Contains("url", result);
        Assert.Contains("https://example.com", result);
        Assert.Contains("thumbnail_url", result);
        Assert.Contains("https://example.com/thumbnail.jpg", result);
        Assert.Contains("information_protection_label", result);
        Assert.Contains("confidential", result);
        Assert.Contains("\"oauth_card_path", result);
        Assert.Contains("oauthCard.json", result);
        Assert.Contains("\"confirmation\":", result);
        Assert.Contains("type", result);
        Assert.Contains("modal", result);
        Assert.Contains("title", result);
        Assert.Contains("Confirm action", result);
        Assert.Contains("body", result);
        Assert.Contains("Do you want to proceed?", result);
        Assert.Contains("\"security_info\":", result);
        Assert.Contains("data_handling", result);
        Assert.Contains("some data handling", result);

    }
}
