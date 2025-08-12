using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.OpenApiExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Moq;
using Xunit;

namespace ReQuesty.Builder.Tests.OpenApiExtensions;
public sealed class OpenApiAiReasoningInstructionsExtensionTest : IDisposable
{
    private readonly HttpClient _httpClient = new();
    public void Dispose()
    {
        _httpClient.Dispose();
    }
    [Fact]
    public void Parses()
    {
        OpenApiAiAuthReferenceIdExtension value = OpenApiAiAuthReferenceIdExtension.Parse("12345");
        Assert.NotNull(value);
        Assert.Equal("12345", value.AuthenticationReferenceId);
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
      security:
        - GraphOAuth2AuthAppOnly:
            - user.read.all
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
      security:
        - GraphOAuth2AuthDelegated:
            - user.read
security:
  - GraphOAuth2AuthDelegated:
      - user.read
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
          type: string
  securitySchemes:
    GraphOAuth2AuthDelegated:
      type: oauth2
      flows:
        authorizationCode:
          authorizationUrl: https://login.microsoftonline.com/tenantid/oauth2/v2.0/authorize
          tokenUrl: https://login.microsoftonline.com/tenantid/oauth2/v2.0/token
          scopes:
            user.read: Grants read access to the signed-in user's profile
      x-ai-auth-reference-id: someValue789
    GraphOAuth2AuthAppOnly:
      type: oauth2
      flows:
        clientCredentials:
          tokenUrl: https://login.microsoftonline.com/tenantid/oauth2/v2.0/token
          scopes:
            user.read.all: Grants read access to all users' full profiles
      x-ai-auth-reference-id: 'otherValue123'";

        Directory.CreateDirectory(TempDirectory);
        string documentPath = Path.Combine(TempDirectory, "document.yaml");
        await File.WriteAllTextAsync(documentPath, documentContent);
        Mock<ILogger<OpenApiDescriptionForModelExtension>> mockLogger = new();
        OpenApiDocumentDownloadService documentDownloadService = new(_httpClient, mockLogger.Object);
        GenerationConfiguration generationConfig = new() { OutputPath = TempDirectory, PluginTypes = [PluginType.APIPlugin] };
        (Stream openApiDocumentStream, bool _) = await documentDownloadService.LoadStreamAsync(documentPath, generationConfig);
        OpenApiDocument? document = await documentDownloadService.GetDocumentFromStreamAsync(openApiDocumentStream, generationConfig);
        Assert.NotNull(document);
        Assert.NotEmpty(document.Components!.SecuritySchemes!);
        Assert.Equal(2, document.Components.SecuritySchemes!.Count);
        IOpenApiSecurityScheme firstSecurityScheme = document.Components.SecuritySchemes.First().Value;
        Assert.True(firstSecurityScheme.Extensions!.TryGetValue(OpenApiAiAuthReferenceIdExtension.Name, out IOpenApiExtension? authReferenceIdExtension));
        Assert.IsType<OpenApiAiAuthReferenceIdExtension>(authReferenceIdExtension);
        Assert.Equal("someValue789", ((OpenApiAiAuthReferenceIdExtension)authReferenceIdExtension).AuthenticationReferenceId);
    }
    [Fact]
    public void Serializes()
    {
        OpenApiAiAuthReferenceIdExtension value = new()
        {
            AuthenticationReferenceId = "refid123",
        };
        using StringWriter sWriter = new();
        OpenApiJsonWriter writer = new(sWriter, new OpenApiJsonWriterSettings { Terse = true });


        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        string result = sWriter.ToString();
        Assert.Equal("\"refid123\"", result);
    }
}
