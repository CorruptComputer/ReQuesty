using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Export;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ReQuesty.Builder.Tests.Export;

public class PublicApiExportServiceTests
{
  private readonly HttpClient _httpClient = new();
  private static Task<Stream> GetTestDocumentStreamAsync()
  {
    return ReQuestyBuilderTests.GetDocumentStreamAsync(@"openapi: 3.0.0
info:
  title: Microsoft Graph get user API
  version: 1.0.0
servers:
  - url: https://graph.microsoft.com/v1.0/
paths:
  /me:
    get:
      responses:
        200:
          description: Success!
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.user'
  /me/get:
    get:
      responses:
        200:
          description: Success!
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.user'
components:
  schemas:
    microsoft.graph.user:
      type: object
      required:
        - id
      properties:
        id:
          type: string
        displayName:
          type: string
        otherNames:
          type: array
          items:
            type: string
            nullable: true
        importance:
          $ref: '#/components/schemas/microsoft.graph.importance'
    microsoft.graph.importance:
      title: importance
      enum:
        - low
        - normal
        - high
      type: string");
  }

  [Fact]
  public void Defensive()
  {
    Assert.Throws<ArgumentNullException>(() => new PublicApiExportService(null!));
  }

  [Fact]
  public async Task GeneratesExportsAndFileHasExpectedAssertionsAsync()
  {
        string tempFilePath = Path.GetTempFileName();
    await using Stream testDocumentStream = await GetTestDocumentStreamAsync();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        GenerationConfiguration generationConfig = new()
        {
      ClientClassName = "Graph",
      OpenAPIFilePath = tempFilePath,
      Language = GenerationLanguage.CSharp,
      ClientNamespaceName = "exportNamespace",
      OutputPath = Path.GetTempPath()
    };
        ReQuestyBuilder builder = new(mockLogger.Object, generationConfig, _httpClient);
        Microsoft.OpenApi.OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(testDocumentStream);

    Assert.NotNull(document);

        Microsoft.OpenApi.OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        Builder.CodeDOM.CodeNamespace codeModel = builder.CreateSourceModel(node);
    await builder.ApplyLanguageRefinementAsync(generationConfig, codeModel, default);

        // serialize the dom model
        PublicApiExportService exportService = new(generationConfig);
    using MemoryStream outputStream = new();
    await exportService.SerializeDomAsync(outputStream, codeModel);

    // validate the export exists
    outputStream.Seek(0, SeekOrigin.Begin);
    Assert.NotEqual(0, outputStream.Length); // output is not empty

    using StreamReader streamReader = new(outputStream);
        HashSet<string> contents = new((await streamReader.ReadToEndAsync()).Split(Environment.NewLine), StringComparer.Ordinal);

    Assert.NotEmpty(contents);
    Assert.Contains("ExportNamespace.Graph-->BaseRequestBuilder", contents); // captures class inheritance
    Assert.Contains("ExportNamespace.Models.Microsoft.Graph.user~~>IAdditionalDataHolder; IParsable", contents);// captures implemented interfaces
    Assert.Contains("ExportNamespace.Models.Microsoft.Graph.user::|public|Id:string", contents);// captures property location,type and access
    Assert.Contains("ExportNamespace.Me.Get.getRequestBuilder::|public|constructor(rawUrl:string; requestAdapter:IRequestAdapter):void", contents); // captures constructors, their parameters(name and types), return and access
    Assert.Contains("ExportNamespace.Me.Get.getRequestBuilder::|public|ToGetRequestInformation(requestConfiguration?:Action<RequestConfiguration<DefaultQueryParameters>>):RequestInformation", contents);// captures methods, their parameters(name and types), return and access
    Assert.Contains("ExportNamespace.Models.Microsoft.Graph.user::|static|public|CreateFromDiscriminatorValue(parseNode:IParseNode):global.ExportNamespace.Models.Microsoft.Graph.User", contents);// captures static methods too :)
    Assert.Contains("ExportNamespace.Models.Microsoft.Graph.importance::0000-low", contents);// captures enum members
    Assert.Contains("ExportNamespace.Models.Microsoft.Graph.user::|public|OtherNames:List<string>", contents);// captures collection info in language specific format
  }
}
