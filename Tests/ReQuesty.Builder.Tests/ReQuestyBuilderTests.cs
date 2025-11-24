using System.Text;
using System.Text.RegularExpressions;

using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Extensions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.MicrosoftExtensions;

using Moq;

using Xunit;
using NetHttpMethod = System.Net.Http.HttpMethod;

namespace ReQuesty.Builder.Tests;

public sealed partial class ReQuestyBuilderTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    public void Dispose()
    {
        foreach (string file in _tempFiles)
        {
            File.Delete(file);
        }

        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public async Task CreateOpenApiDocumentWithResultAsync_ReturnsDiagnostics()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.4
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /samplepath:
    get:
      description: response description
      responses:
        '200':
          content:
            application/json:
              schema:
                type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        Microsoft.OpenApi.Reader.ReadResult? readResult = await builder.CreateOpenApiDocumentWithResultAsync(fs);
        Assert.NotNull(readResult);
        OpenApiDocument? document = readResult?.Document;
        Microsoft.OpenApi.Reader.OpenApiDiagnostic? diagnostics = readResult?.Diagnostic;
        Assert.NotNull(document);
        Assert.NotNull(diagnostics);
        Assert.Equal(OpenApiSpecVersion.OpenApi3_0, diagnostics.SpecificationVersion);
    }
    [Fact]
    public async Task SupportsExternalReferences()
    {
        string tempFilePathReferee = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePathReferee,
    """
openapi: 3.1.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /placeholder:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: string
components:
  schemas:
    MySchema:
      type: object
      properties:
        id:
          type: string
""");
        string tempFilePathReferrer = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePathReferrer,
    $$$"""
openapi: 3.1.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /placeholder:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '{{{tempFilePathReferee}}}#/components/schemas/MySchema'
components:
  schemas:
    MySchema:
      type: object
      properties:
        id:
          type: string
""");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePathReferrer, Serializers = ["none"], Deserializers = ["none"] }, _httpClient);
        await using FileStream fs = new(tempFilePathReferrer, FileMode.Open);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        CodeClass? modelClass = rootNS.FindChildByName<CodeClass>("MySchema", true);
        Assert.NotNull(modelClass);
        Assert.Single(modelClass.Properties, static x => x.Name.Equals("id", StringComparison.OrdinalIgnoreCase));
    }
    [InlineData("https://graph.microsoft.com/description.yaml", "/v1.0", "https://graph.microsoft.com/v1.0")]
    [InlineData("/home/vsts/a/s/1", "/v1.0", "/v1.0")]
    [InlineData("https://graph.microsoft.com/docs/description.yaml", "../v1.0", "https://graph.microsoft.com/v1.0")]
    [InlineData("https://graph.microsoft.com/description.yaml", "https://graph.microsoft.com/v1.0", "https://graph.microsoft.com/v1.0")]
    [Theory]
    public async Task SupportsRelativeServerUrlAsync(string descriptionUrl, string serverRelativeUrl, string expected)
    {
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, @$"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: {serverRelativeUrl}
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = descriptionUrl }, _httpClient);
        await using FileStream fs = new(tempFilePath, FileMode.Open);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        CodeClass? clientBuilder = rootNS.FindChildByName<CodeClass>("Graph", false);
        Assert.NotNull(clientBuilder);
        CodeMethod? constructor = clientBuilder.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
        Assert.NotNull(constructor);
        Assert.Equal(expected, constructor.BaseUrl);
    }
    [Fact]
    public async Task HonoursNoneKeyForSerializationAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, @$"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = "https://graph.microsoft.com/description.yaml", Serializers = ["none"], Deserializers = ["none"] }, _httpClient);
        await using FileStream fs = new(tempFilePath, FileMode.Open);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        CodeClass? clientBuilder = rootNS.FindChildByName<CodeClass>("Graph", false);
        Assert.NotNull(clientBuilder);
        CodeMethod? constructor = clientBuilder.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
        Assert.NotNull(constructor);
        Assert.Empty(constructor.SerializerModules);
        Assert.Empty(constructor.DeserializerModules);
    }
    [Fact]
    public async Task DeduplicatesHostNamesAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, @$"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: http://api.funtranslations.com
  - url: https://api.funtranslations.com
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = "https://api.apis.guru/v2/specs/funtranslations.com/starwars/2.3/swagger.json" }, _httpClient);
        await using FileStream fs = new(tempFilePath, FileMode.Open);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        CodeClass? clientBuilder = rootNS.FindChildByName<CodeClass>("Graph", false);
        Assert.NotNull(clientBuilder);
        CodeMethod? constructor = clientBuilder.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
        Assert.NotNull(constructor);
        Assert.Equal("https://api.funtranslations.com", constructor.BaseUrl);
    }
    [Fact]
    public async Task DeduplicatesHostNamesWithOpenAPI2Async()
    {
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, @$"swagger: 2.0
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
schemes:
  - https
  - http
host: api.funtranslations.com
basePath: /
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = "https://api.apis.guru/v2/specs/funtranslations.com/starwars/2.3/swagger.json" }, _httpClient);
        await using FileStream fs = new(tempFilePath, FileMode.Open);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        CodeClass? clientBuilder = rootNS.FindChildByName<CodeClass>("Graph", false);
        Assert.NotNull(clientBuilder);
        CodeMethod? constructor = clientBuilder.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
        Assert.NotNull(constructor);
        Assert.Equal("https://api.funtranslations.com", constructor.BaseUrl);
    }
    [Fact]
    public async Task HandlesSpecialCharactersInPathSegmentAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, @$"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://api.funtranslations.com
paths:
  /my-api:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Specialized-Complex.StorageAccount'
components:
  schemas:
    Specialized-Complex.StorageAccount:
      type: object
      properties:
        name:
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = "https://api.apis.guru/v2/specs/funtranslations.com/starwars/2.3/swagger.json" }, _httpClient);
        await using FileStream fs = new(tempFilePath, FileMode.Open);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        Assert.Null(codeModel.FindNamespaceByName("ApiSdk.my-api"));
        Assert.NotNull(codeModel.FindNamespaceByName("ApiSdk.MyApi"));
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNS);
        CodeNamespace? specializedNS = modelsNS.FindNamespaceByName("ApiSdk.models.SpecializedComplex");
        Assert.NotNull(specializedNS);
        Assert.Null(modelsNS.FindNamespaceByName("ApiSdk.models.Specialized-Complex"));
        Assert.NotNull(specializedNS.FindChildByName<CodeClass>("StorageAccount", false));
    }
    [Fact]
    public async Task HandlesPathWithRepeatedSegment()
    {
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, @$"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://api.funtranslations.com
paths:
  /media/response/response/{{id}}:
    get:
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/MediaResponseModel'
components:
  schemas:
    MediaResponseModel:
      type: object
      properties:
        name:
          type: string
        id:
          type: string
          format: uuid
        mediaType:
          type: string
        url:
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration
        {
            ClientClassName = "Graph",
            OpenAPIFilePath = "https://api.apis.guru/v2/specs/funtranslations.com/starwars/2.3/swagger.json"
        }, _httpClient);
        await using FileStream fs = new(tempFilePath, FileMode.Open);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        CodeNamespace? responseBuilderNs = codeModel.FindNamespaceByName("ApiSdk.media.response");
        Assert.NotNull(responseBuilderNs);
        CodeClass? responseRequestBuilder = responseBuilderNs.FindChildByName<CodeClass>("ResponseRequestBuilder", false);
        Assert.NotNull(responseRequestBuilder);
        CodeProperty? navigationProperty = responseRequestBuilder.Properties.FirstOrDefault(prop =>
            prop.IsOfKind(CodePropertyKind.RequestBuilder) &&
            prop.Name.Equals("Response", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(navigationProperty);
        CodeType? navigationPropertyType = navigationProperty.Type as CodeType;
        Assert.NotNull(navigationPropertyType);
        Assert.NotEqual(responseRequestBuilder, navigationPropertyType.TypeDefinition);// the request builder should not be the same as the class it is in.
        CodeNamespace? nestedResponseBuilderNs = codeModel.FindNamespaceByName("ApiSdk.media.response.response");
        Assert.NotNull(nestedResponseBuilderNs);
        CodeClass? nestedResponseRequestBuilder = nestedResponseBuilderNs.FindChildByName<CodeClass>("ResponseRequestBuilder", false);
        Assert.Equal(nestedResponseRequestBuilder, navigationPropertyType.TypeDefinition);// the request builder should not be the same as the class it is in.
    }
    [Fact]
    public async Task HandlesPathWithItemInNameSegment()
    {
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, @$"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://api.funtranslations.com
paths:
  /media/item/{{id}}:
    get:
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/MediaResponseModel'
  /media/item/{{id}}/nestedItem:
    get:
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/MediaResponseModel'
components:
  schemas:
    MediaResponseModel:
      type: object
      properties:
        name:
          type: string
        id:
          type: string
          format: uuid
        mediaType:
          type: string
        url:
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration
        {
            ClientClassName = "Graph",
            OpenAPIFilePath = "https://api.apis.guru/v2/specs/funtranslations.com/starwars/2.3/swagger.json"
        }, _httpClient);
        await using FileStream fs = new(tempFilePath, FileMode.Open);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        CodeNamespace? mediaBuilderNs = codeModel.FindNamespaceByName("ApiSdk.media");
        Assert.NotNull(mediaBuilderNs);
        CodeClass? mediaRequestBuilder = mediaBuilderNs.FindChildByName<CodeClass>("MediaRequestBuilder", false);
        Assert.NotNull(mediaRequestBuilder);
        CodeProperty? navigationProperty = mediaRequestBuilder.Properties.FirstOrDefault(prop =>
            prop.IsOfKind(CodePropertyKind.RequestBuilder) &&
            prop.Name.Equals("Item", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(navigationProperty);
        Assert.Equal("Item_EscapedRequestBuilder", navigationProperty.Type.Name);
        CodeNamespace? itemBuilderNs = mediaBuilderNs.FindNamespaceByName("ApiSdk.media.item_escaped");
        Assert.NotNull(itemBuilderNs);
        CodeClass? itemRequestBuilder = itemBuilderNs.FindChildByName<CodeClass>("Item_escapedRequestBuilder", false);
        Assert.NotNull(itemRequestBuilder);
        Assert.NotNull(itemRequestBuilder.Indexer);
        Assert.Equal("ItemItemRequestBuilder", itemRequestBuilder.Indexer.ReturnType.Name);
        CodeNamespace? nestedItemBuilderNs = itemBuilderNs.FindNamespaceByName("ApiSdk.media.item_escaped.item");
        Assert.NotNull(nestedItemBuilderNs);
        CodeClass? nestedItemRequestBuilder = nestedItemBuilderNs.FindChildByName<CodeClass>("ItemItemRequestBuilder", false);
        Assert.NotNull(nestedItemRequestBuilder);
        Assert.NotNull(nestedItemRequestBuilder.Methods.FirstOrDefault(m =>
            m.HttpMethod == Builder.CodeDOM.HttpMethod.Get &&
            m.IsAsync &&
            m.Name.Equals("Get", StringComparison.OrdinalIgnoreCase)));
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNS);
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("MediaResponseModel", false));
        CodeProperty? nestedNestedItemProperty =
            nestedItemRequestBuilder.FindChildByName<CodeProperty>("NestedItem", false);
        Assert.NotNull(nestedNestedItemProperty);
        Assert.Equal("NestedItemRequestBuilder", nestedNestedItemProperty.Type.Name, StringComparer.OrdinalIgnoreCase);
    }
    private readonly HttpClient _httpClient = new();
    [Fact]
    public async Task ParsesEnumDescriptionsAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/StorageAccount'
components:
  schemas:
    StorageAccount:
      type: object
      properties:
        accountType:
          $ref: '#/components/schemas/StorageAccountType'
    StorageAccountType:
      type: string
      enum:
        - '+1'
        - -1
        - Standard_LRS
        - Standard_ZRS
        - Standard_GRS
        - Standard_RAGRS
        - Premium_LRS
        - Premium_LRS
      x-ms-enum:
        name: AccountType
        modelAsString: false
        values:
          - value: '+1'
          - value: -1
          - value: Standard_LRS
            description: Locally redundant storage.
            name: StandardLocalRedundancy
          - value: Standard_ZRS
            description: Zone-redundant storage.
          - value: Standard_GRS
            name: StandardGeoRedundancy
          - value: Standard_RAGRS
          - value: Premium_LRS");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNS);
        CodeEnum? enumDef = modelsNS.FindChildByName<CodeEnum>("StorageAccountType", false);
        Assert.NotNull(enumDef);
        Assert.False(enumDef.Flags);
        CodeEnumOption firstOption = enumDef.Options.First();
        Assert.Equal("+1", firstOption.SerializationName);
        Assert.Equal("plus_1", firstOption.Name);
        Assert.Empty(firstOption.Documentation.DescriptionTemplate);
        CodeEnumOption secondOption = enumDef.Options.ElementAt(1);
        Assert.Equal("-1", secondOption.SerializationName);
        Assert.Equal("minus_1", secondOption.Name);
        Assert.Empty(secondOption.Documentation.DescriptionTemplate);
        CodeEnumOption thirdOption = enumDef.Options.ElementAt(2);
        Assert.Equal("Standard_LRS", thirdOption.SerializationName);
        Assert.Equal("StandardLocalRedundancy", thirdOption.Name);
        Assert.NotEmpty(thirdOption.Documentation.DescriptionTemplate);
        Assert.Single(enumDef.Options, static x => x.Name.Equals("Premium_LRS", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ParsesEnumFlagsInformationAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/StorageAccount'
components:
  schemas:
    StorageAccount:
      type: object
      properties:
        accountType:
          $ref: '#/components/schemas/StorageAccountType'
    StorageAccountType:
      type: string
      enum:
        - Standard_LRS
        - Standard_ZRS
        - Standard_GRS
        - Standard_RAGRS
        - Premium_LRS
        - Premium_LRS
      x-ms-enum-flags:
        isFlags: true
        style: simple");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("ApiSdk.models");
        Assert.NotNull(modelsNS);
        CodeEnum? enumDef = modelsNS.FindChildByName<CodeEnum>("StorageAccountType", false);
        Assert.NotNull(enumDef);
        Assert.True(enumDef.Flags);
    }
    [Fact]
    public async Task DoesntConflictOnModelsNamespaceAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /models:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.directoryObject'
  /models/inner:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.directoryObject'
components:
  schemas:
    microsoft.graph.directoryObject:
      title: directoryObject
      type: object
      properties:
        deletedDateTime:
          type: string
          pattern: '^[0-9]{4,}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]{1,12})?(Z|[+-][0-9][0-9]:[0-9][0-9])$'
          format: date-time
          nullable: true");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("ApiSdk.models.microsoft.graph");
        Assert.NotNull(modelsNS);
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("DirectoryObject", false));
        Assert.Null(modelsNS.FindChildByName<CodeClass>("ModelsRequestRequestBuilder", false));
        CodeNamespace? requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.modelsRequests");
        Assert.NotNull(requestBuilderNS);
        Assert.NotNull(requestBuilderNS.FindChildByName<CodeClass>("ModelsRequestBuilder", false));
        Assert.Null(requestBuilderNS.FindChildByName<CodeClass>("DirectoryObject", false));
        CodeNamespace? innerRequestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.modelsRequests.inner");
        Assert.NotNull(innerRequestBuilderNS);
        Assert.NotNull(innerRequestBuilderNS.FindChildByName<CodeClass>("InnerRequestBuilder", false));

    }

    [Fact]
    public async Task DoesNotAddSuperflousFieldsToModelsAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.3
info:
  title: Example API
  version: 1.0.0
servers:
  - url: ""https://localhost:8080""
paths:
  ""/api/all"":
    post:
      requestBody:
        content:
          application/json:
            schema:
              $ref: ""#/components/schemas/AorB""
        required: true
      responses:
        ""200"":
          $ref: ""#/components/responses/AorBResponse""
components:
  schemas:
    A:
      type: object
      required:
        - type
      properties:
        type:
          type: string
          default: ""a""
    B:
      type: object
      required:
        - type
      properties:
        type:
          type: string
          default: ""b""
    AorB:
      oneOf:
        - $ref: ""#/components/schemas/A""
        - $ref: ""#/components/schemas/B""
      discriminator:
        propertyName: type
        mapping:
          a: ""#/components/schemas/A""
          b: ""#/components/schemas/B""
  responses:
    AorBResponse:
      description: mandatory
      content:
        application/json:
          schema:
            $ref: ""#/components/schemas/AorB""");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        GenerationConfiguration generationConfiguration = new() { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, Language = GenerationLanguage.CSharp }; // we can use any language that creates wrapper types for composed types in different ways
        ReQuestyBuilder builder = new(mockLogger.Object, generationConfiguration, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        await builder.ApplyLanguageRefinementAsync(generationConfiguration, codeModel, CancellationToken.None);
        CodeNamespace? requestBuilderNamespace = codeModel.FindNamespaceByName("ApiSdk.api.all");
        Assert.NotNull(requestBuilderNamespace);

        CodeClass? allRequestBuilderClass = requestBuilderNamespace.FindChildByName<CodeClass>("allRequestBuilder", false);
        Assert.NotNull(allRequestBuilderClass);
        CodeMethod? executor = allRequestBuilderClass.Methods.FirstOrDefault(m => m.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executor);
        CodeType? returnType = executor.ReturnType as CodeType;
        Assert.NotNull(returnType);
        CodeClass? returnTypeDefinition = returnType.TypeDefinition as CodeClass;
        Assert.NotNull(returnTypeDefinition);
        Assert.Equal(2, returnTypeDefinition.Properties.Count());
    }

    [Fact]
    public async Task NamesComponentsInlineSchemasProperlyAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /users:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.directoryObject'
components:
  schemas:
    microsoft.graph.directoryObject:
      title: directoryObject
      type: object
      properties:
        deletedDateTime:
          oneOf:
            - type: string
              pattern: '^[0-9]{4,}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]{1,12})?(Z|[+-][0-9][0-9]:[0-9][0-9])$'
              format: date-time
              nullable: true
            - type: number
              format: int64
            - type: object
              properties:
                day:
                  type: integer
                  format: int32
                month:
                  type: integer
                  format: int32
                year:
                  type: integer
                  format: int32");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("ApiSdk.models.microsoft.graph");
        Assert.NotNull(modelsNS);
        CodeClass? doClass = modelsNS.FindChildByName<CodeClass>("DirectoryObject", false);
        Assert.NotNull(doClass);
        CodeProperty? deletedDateTimeProperty = doClass.FindChildByName<CodeProperty>("DeletedDateTime", false);
        Assert.NotNull(deletedDateTimeProperty);
        CodeUnionType unionType = Assert.IsType<CodeUnionType>(deletedDateTimeProperty.Type);
        Assert.Equal("directoryObject_deletedDateTime", unionType.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(3, unionType.Types.Count());
        Assert.Single(unionType.Types, t => "DateTimeOffset".Equals(t.Name, StringComparison.OrdinalIgnoreCase));
        Assert.Single(unionType.Types, t => "directoryObject_deletedDateTimeMember1".Equals(t.Name, StringComparison.OrdinalIgnoreCase));
        Assert.Single(unionType.Types, t => "int64".Equals(t.Name, StringComparison.OrdinalIgnoreCase));
        Assert.Null(modelsNS.FindChildByName<CodeClass>("users"));
    }
    [Theory]
    [InlineData("description: 'Represents an Azure Active Directory user.'")]
    [InlineData("title: 'user'")]
    [InlineData("default: {\"displayName\": \"displayName-value\"}")]
    [InlineData("examples: {\"displayName\": \"displayName-value\"}")]
    [InlineData("readOnly: true")]
    [InlineData("writeOnly: true")]
    [InlineData("deprecated: true")]
    public async Task DoesNotIntroduceIntermediateTypesForMeaninglessPropertiesAsync(string additionalInformation)
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /users:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                allOf:
                  - $ref: '#/components/schemas/microsoft.graph.directoryObject'
                  - " + additionalInformation + @"
components:
  schemas:
    microsoft.graph.directoryObject:
      title: directoryObject
      required:
        - '@odata.type'
      type: object
      properties:
        deletedDateTime:
          pattern: '^[0-9]{4,}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]{1,12})?(Z|[+-][0-9][0-9]:[0-9][0-9])$'
          type: string
          format: date-time
          nullable: true
        '@odata.type':
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("ApiSdk.models.microsoft.graph");
        Assert.NotNull(modelsNS);
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("DirectoryObject", false)); //type in use
        CodeNamespace? usersNS = codeModel.FindNamespaceByName("ApiSdk.users");
        Assert.NotNull(usersNS);
        CodeClass? usersRB = usersNS.FindChildByName<CodeClass>("UsersRequestBuilder", false);
        Assert.NotNull(usersRB);
        CodeMethod? getMethod = usersRB.FindChildByName<CodeMethod>("Get", false);
        Assert.NotNull(getMethod);
        Assert.Equal("DirectoryObject", getMethod.ReturnType.Name, StringComparer.OrdinalIgnoreCase); //type in use
        Assert.Null(modelsNS.FindChildByName<CodeClass>("UsersResponse", false)); //empty type
    }
    [Fact]
    public async Task TrimsInheritanceUnusedModelsAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.directoryObject'
components:
  schemas:
    microsoft.graph.entity:
      title: entity
      required:
        - '@odata.type'
      type: object
      properties:
        id:
          type: string
        '@odata.type':
          type: string
      discriminator:
        propertyName: '@odata.type'
        mapping:
          '#microsoft.graph.auditEvent': '#/components/schemas/microsoft.graph.auditEvent'
          '#microsoft.graph.directoryObject': '#/components/schemas/microsoft.graph.directoryObject'
      x-ms-discriminator-value: '#microsoft.graph.entity'
    microsoft.graph.auditEvent:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.entity'
        - title: auditEvent
          required:
            - '@odata.type'
          type: object
          properties:
            eventDateTime:
              pattern: '^[0-9]{4,}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]{1,12})?(Z|[+-][0-9][0-9]:[0-9][0-9])$'
              type: string
              format: date-time
              nullable: true
            '@odata.type':
              type: string
          discriminator:
            propertyName: '@odata.type'
            mapping:
              '#microsoft.graph.user': '#/components/schemas/microsoft.graph.user'
      x-ms-discriminator-value: '#microsoft.graph.auditEvent'
    microsoft.graph.directoryObject:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.entity'
        - title: directoryObject
          required:
            - '@odata.type'
          type: object
          properties:
            deletedDateTime:
              pattern: '^[0-9]{4,}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]{1,12})?(Z|[+-][0-9][0-9]:[0-9][0-9])$'
              type: string
              format: date-time
              nullable: true
            '@odata.type':
              type: string
          discriminator:
            propertyName: '@odata.type'
            mapping:
              '#microsoft.graph.user': '#/components/schemas/microsoft.graph.user'
      x-ms-discriminator-value: '#microsoft.graph.directoryObject'
    microsoft.graph.user:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.directoryObject'
        - title: user
          required:
            - '@odata.type'
          type: object
          properties:
            accountEnabled:
              type: boolean
              nullable: true
            '@odata.type':
              type: string
              default: '#microsoft.graph.user'
            mailboxSettings:
              $ref: '#/components/schemas/microsoft.graph.mailboxSettings'
          discriminator:
            propertyName: '@odata.type'
            mapping:
              '#microsoft.graph.educationUser': '#/components/schemas/microsoft.graph.educationUser'
      x-ms-discriminator-value: '#microsoft.graph.user'
    microsoft.graph.mailboxSettingsBase:
      title: mailboxSettingsBase
      type: object
      properties:
        premium:
          type: boolean
          nullable: true
    microsoft.graph.mailboxSettings:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.mailboxSettingsBase'
        - title: mailboxSettings
          type: object
          properties:
            antiSpamEnabled:
              type: boolean
              nullable: true
    microsoft.graph.mailboxSecuritySettings:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.mailboxSettings'
        - title: mailboxSettings
          type: object
          properties:
            encryptionAtRestEnabled:
              type: boolean
              nullable: true
    microsoft.graph.educationUser:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.user'
        - title: user
          required:
            - '@odata.type'
          type: object
          properties:
            pupilEnrolled:
              type: boolean
              description: 'true if the account is enabled; otherwise, false. This property is required when a user is created. Returned only on $select. Supports $filter (eq, ne, not, and in).'
              nullable: true
            '@odata.type':
              type: string
              default: '#microsoft.graph.educationUser'
      x-ms-discriminator-value: '#microsoft.graph.educationUser'");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("ApiSdk.models.microsoft.graph");
        Assert.NotNull(modelsNS);
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("Entity", false)); //parent type
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("DirectoryObject", false)); //type in use
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("User", false)); //derived type
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("MailboxSettingsBase", false)); //base of a property of a derived type
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("MailboxSecuritySettings", false)); //derived type of a property
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("MailboxSettings", false)); //property of a derived type
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("EducationUser", false)); // recursive downcast
        Assert.Null(modelsNS.FindChildByName<CodeClass>("AuditEvent", false)); //unused type
    }
    [Fact]
    public async Task DisambiguatesReservedPropertiesAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: v1.0
  x-ms-generated-by:
    toolName: Microsoft.OpenApi.OData
    toolVersion: 1.0.9.0
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  '/security/alerts_v2/{alert-id}':
    get:
      responses:
        200:
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.alert'
components:
  schemas:
    microsoft.graph.entity:
      title: entity
      required:
        - '@odata.type'
      type: object
      properties:
        id:
          type: string
        '@odata.type':
          type: string
    microsoft.graph.dictionary:
      title: dictionary
      required:
        - '@odata.type'
      type: object
      properties:
        '@odata.type':
          type: string
    microsoft.graph.alert:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.entity'
        - title: alert
          required:
            - '@odata.type'
          type: object
          properties:
            actorDisplayName:
              type: string
              nullable: true
            additionalData:
              anyOf:
                - $ref: '#/components/schemas/microsoft.graph.dictionary'
                - type: object
                  nullable: true");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("ApiSdk.models.microsoft.graph");
        Assert.NotNull(modelsNS);
        CodeClass? entityClass = modelsNS.FindChildByName<CodeClass>("Entity", false);
        Assert.NotNull(entityClass);
        CodeProperty? additionalDataProperty = entityClass.FindChildByName<CodeProperty>("AdditionalData", false);
        Assert.NotNull(additionalDataProperty);
        Assert.True(additionalDataProperty.Kind is CodePropertyKind.AdditionalData);
        CodeClass? alertClass = modelsNS.FindChildByName<CodeClass>("Alert", false);
        Assert.NotNull(alertClass);
        CodeProperty? additionalDataEscapedProperty = alertClass.FindChildByName<CodeProperty>("AdditionalDataProperty", false);
        Assert.NotNull(additionalDataEscapedProperty);
        Assert.True(additionalDataEscapedProperty.Kind is CodePropertyKind.Custom);
    }
    [Fact]
    public async Task TrimsInheritanceUnusedModelsWithUnionAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                anyOf:
                  - $ref: '#/components/schemas/microsoft.graph.user'
                  - $ref: '#/components/schemas/microsoft.graph.educationUser'
components:
  schemas:
    microsoft.graph.entity:
      title: entity
      required:
        - '@odata.type'
      type: object
      properties:
        id:
          type: string
          description: The unique idenfier for an entity. Read-only.
        '@odata.type':
          type: string
      discriminator:
        propertyName: '@odata.type'
        mapping:
          '#microsoft.graph.auditEvent': '#/components/schemas/microsoft.graph.auditEvent'
          '#microsoft.graph.directoryObject': '#/components/schemas/microsoft.graph.directoryObject'
      x-ms-discriminator-value: '#microsoft.graph.entity'
    microsoft.graph.auditEvent:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.entity'
        - title: auditEvent
          required:
            - '@odata.type'
          type: object
          properties:
            eventDateTime:
              pattern: '^[0-9]{4,}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]{1,12})?(Z|[+-][0-9][0-9]:[0-9][0-9])$'
              type: string
              format: date-time
              nullable: true
            '@odata.type':
              type: string
          discriminator:
            propertyName: '@odata.type'
            mapping:
              '#microsoft.graph.user': '#/components/schemas/microsoft.graph.user'
      x-ms-discriminator-value: '#microsoft.graph.auditEvent'
    microsoft.graph.directoryObject:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.entity'
        - title: directoryObject
          required:
            - '@odata.type'
          type: object
          properties:
            deletedDateTime:
              pattern: '^[0-9]{4,}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]{1,12})?(Z|[+-][0-9][0-9]:[0-9][0-9])$'
              type: string
              description: Date and time when this object was deleted. Always null when the object hasn't been deleted.
              format: date-time
              nullable: true
            '@odata.type':
              type: string
          discriminator:
            propertyName: '@odata.type'
            mapping:
              '#microsoft.graph.user': '#/components/schemas/microsoft.graph.user'
      x-ms-discriminator-value: '#microsoft.graph.directoryObject'
    microsoft.graph.user:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.directoryObject'
        - title: user
          required:
            - '@odata.type'
          type: object
          properties:
            accountEnabled:
              type: boolean
              description: 'true if the account is enabled; otherwise, false. This property is required when a user is created. Returned only on $select. Supports $filter (eq, ne, not, and in).'
              nullable: true
            '@odata.type':
              type: string
              default: '#microsoft.graph.user'
          discriminator:
            propertyName: '@odata.type'
            mapping:
              '#microsoft.graph.educationUser': '#/components/schemas/microsoft.graph.educationUser'
      x-ms-discriminator-value: '#microsoft.graph.user'
    microsoft.graph.educationUser:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.user'
        - title: user
          required:
            - '@odata.type'
          type: object
          properties:
            pupilEnrolled:
              type: boolean
              description: 'true if the account is enabled; otherwise, false. This property is required when a user is created. Returned only on $select. Supports $filter (eq, ne, not, and in).'
              nullable: true
            '@odata.type':
              type: string
              default: '#microsoft.graph.educationUser'
      x-ms-discriminator-value: '#microsoft.graph.educationUser'");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("ApiSdk.models.microsoft.graph");
        Assert.NotNull(modelsNS);
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("Entity", false));
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("DirectoryObject", false));
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("User", false));
        Assert.NotNull(modelsNS.FindChildByName<CodeClass>("EducationUser", false));
        Assert.Null(modelsNS.FindChildByName<CodeClass>("AuditEvent", false));
    }
    internal static async Task<Stream> GetDocumentStreamAsync(string document)
    {
        MemoryStream ms = new();
        await using StreamWriter tw = new(ms, Encoding.UTF8, leaveOpen: true);
        await tw.WriteAsync(document);
        await tw.FlushAsync();
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
    [Fact]
    public async Task ParsesReQuestyExtensionAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
x-requesty-info:
  languagesInformation:
    CSharp:
      dependencyInstallCommand: dotnet add {0} {1}
      dependencies:
        - name: Microsoft.Graph.Core
          version: 3.0.0
servers:
  - url: https://graph.microsoft.com/v1.0");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        LanguagesInformation? extensionResult = await builder.GetLanguagesInformationAsync(new CancellationToken());
        Assert.NotNull(extensionResult);
        Assert.True(extensionResult.TryGetValue("CSharp", out LanguageInformation? csharpInfo));
        Assert.Equal("dotnet add {0} {1}", csharpInfo.DependencyInstallCommand);
        Assert.Single(csharpInfo.Dependencies);
        Assert.Equal("Microsoft.Graph.Core", csharpInfo.Dependencies.First().Name);
        Assert.Equal("3.0.0", csharpInfo.Dependencies.First().Version);
    }
    [Fact]
    public async Task UpdatesGenerationConfigurationFromInformationAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
x-requesty-info:
  languagesInformation:
    CSharp:
      dependencyInstallCommand: dotnet add {0} {1}
      clientClassName: GraphClient
      clientNamespaceName: Microsoft.Graph
      structuredMimeTypes:
        - application/json
        - application/xml
      dependencies:
        - name: Microsoft.Graph.Core
          version: 3.0.0
servers:
  - url: https://graph.microsoft.com/v1.0");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        GenerationConfiguration configuration = new() { OpenAPIFilePath = tempFilePath, Language = GenerationLanguage.CSharp };
        ReQuestyBuilder builder = new(mockLogger.Object, configuration, _httpClient);
        (OpenApiUrlTreeNode? treeNode, Microsoft.OpenApi.Reader.OpenApiDiagnostic? _) = await builder.GetUrlTreeNodeAsync(new CancellationToken());
        Assert.NotNull(treeNode);
        Assert.Equal("GraphClient", configuration.ClientClassName);
        Assert.Equal("Microsoft.Graph", configuration.ClientNamespaceName);
        Assert.Contains("application/json", configuration.StructuredMimeTypes);
        Assert.Contains("application/xml", configuration.StructuredMimeTypes);
        _tempFiles.Add(tempFilePath);
    }
    [Fact]
    public async Task DoesntFailOnEmptyReQuestyExtensionAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        LanguagesInformation? extensionResult = await builder.GetLanguagesInformationAsync(new CancellationToken());
        Assert.Null(extensionResult);
    }
    [Fact]
    public async Task DoesntFailOnParameterWithoutSchemaReQuestyExtensionAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  '/users/{user-id}':
    get:
      parameters:
      - name: user-id
        in: path");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, Language = GenerationLanguage.CSharp }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace model = builder.CreateSourceModel(node);
        Assert.NotNull(model);
    }
    [Fact]
    public async Task GetsUrlTreeNodeAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        (OpenApiUrlTreeNode? treeNode, Microsoft.OpenApi.Reader.OpenApiDiagnostic? _) = await builder.GetUrlTreeNodeAsync(new CancellationToken());
        Assert.NotNull(treeNode);
        Assert.Equal("/", treeNode.DeduplicatedSegment());
        Assert.Equal("enumeration", treeNode.Children.First().Value.DeduplicatedSegment());

        _tempFiles.Add(tempFilePath);
    }
    [Fact]
    public async Task DoesntThrowOnMissingServerForV2Async()
    {
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tempFilePath, ["swagger: 2.0", "title: \"Todo API\"", "version: \"1.0.0\"", "host: mytodos.doesntexit", "basePath: v2", "schemes:", " - https", " - http"]);
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        await builder.GenerateClientAsync(new());
        _tempFiles.Add(tempFilePath);
    }
    [Fact]
    public void Single_root_node_creates_single_request_builder_class()
    {
        OpenApiUrlTreeNode node = OpenApiUrlTreeNode.Create();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        CodeNamespace codeModel = builder.CreateSourceModel(node);

        Assert.Single(codeModel.GetChildElements(true));
    }
    [Fact]
    public void Single_path_with_get_collection()
    {
        OpenApiUrlTreeNode node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = new()
            {
                [NetHttpMethod.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.Array,
                                        Items = new OpenApiSchema
                                        {
                                            Type = JsonSchemaType.Integer
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        CodeNamespace codeModel = builder.CreateSourceModel(node);

        CodeElement rootNamespace = codeModel.GetChildElements(true).Single();
        CodeClass rootBuilder = rootNamespace.GetChildElements(true).OfType<CodeClass>().Single(e => e.Name == "Graph");
        CodeProperty tasksProperty = rootBuilder.Properties.Single(e => e.Name.Equals("Tasks", StringComparison.OrdinalIgnoreCase));
        CodeType tasksRequestBuilder = Assert.IsType<CodeType>(tasksProperty.Type);
        CodeClass classTypeDefinition = Assert.IsType<CodeClass>(tasksRequestBuilder.TypeDefinition);
        CodeMethod getMethod = classTypeDefinition.Methods.Single(e => e.Name == "Get");
        CodeTypeBase returnType = getMethod.ReturnType;
        Assert.Equal(CodeTypeBase.CodeTypeCollectionKind.Complex, returnType.CollectionKind);
    }
    [Fact]
    public void OData_doubles_as_one_of()
    {
        OpenApiUrlTreeNode node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = new()
            {
                [NetHttpMethod.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.Object,
                                        Properties = new Dictionary<string, IOpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    OneOf = [
                                                        new OpenApiSchema{
                                                            Type = JsonSchemaType.Number
                                                        },
                                                        new OpenApiSchema{
                                                            Type = JsonSchemaType.String
                                                        },
                                                        new OpenApiSchema {
                                                            Enum = ["-INF", "INF", "NaN"]
                                                        }
                                                    ],
                                                    Format = "double"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeProperty? progressProp = codeModel.FindChildByName<CodeProperty>("progress");
        Assert.NotNull(progressProp);
        Assert.Equal("double", progressProp.Type.Name);
    }
    [Fact]
    public void OData_doubles_as_one_of_format_inside()
    {
        OpenApiUrlTreeNode node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = new()
            {
                [NetHttpMethod.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.Object,
                                        Properties = new Dictionary<string, IOpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    OneOf = [
                                                        new OpenApiSchema{
                                                            Type = JsonSchemaType.Number,
                                                            Format = "double"
                                                        },
                                                        new OpenApiSchema{
                                                            Type = JsonSchemaType.String
                                                        },
                                                        new OpenApiSchema {
                                                            Enum = ["-INF", "INF", "NaN"]
                                                        }
                                                    ],
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeProperty? progressProp = codeModel.FindChildByName<CodeProperty>("progress");
        Assert.NotNull(progressProp);
        Assert.Equal("double", progressProp.Type.Name);
    }
    [Fact]
    public void OData_doubles_as_any_of()
    {
        OpenApiUrlTreeNode node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = new()
            {
                [NetHttpMethod.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.Object,
                                        Properties = new Dictionary<string, IOpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    AnyOf = [
                                                        new OpenApiSchema{
                                                            Type = JsonSchemaType.Number
                                                        },
                                                        new OpenApiSchema{
                                                            Type = JsonSchemaType.String
                                                        },
                                                        new OpenApiSchema {
                                                            Enum = ["-INF", "INF", "NaN"]
                                                        }
                                                    ],
                                                    Format = "double"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeProperty? progressProp = codeModel.FindChildByName<CodeProperty>("progress");
        Assert.NotNull(progressProp);
        Assert.Equal("double", progressProp.Type.Name);
    }
    [Fact]
    public void MultiNestedArraysSupportedAsUntypedNodes()
    {
        OpenApiSchema fooSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "sortBy", new OpenApiSchema {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema {
                            Type = JsonSchemaType.Array,
                            Items = new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        }
                    }
                },
            }
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["foos/{id}"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("bar.foo")
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
        };
        document.AddComponent("bar.foo", fooSchema);
        document.SetReferenceHostDocument();
        CountLogger<ReQuestyBuilder> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? fooClass = codeModel.FindNamespaceByName("ApiSdk.models")?.FindChildByName<CodeClass>("foo");
        Assert.NotNull(fooClass);
        CodeProperty? sortByProp = fooClass.FindChildByName<CodeProperty>("sortBy", false);
        Assert.NotNull(sortByProp);
        Assert.Equal(ReQuestyBuilder.UntypedNodeName, sortByProp.Type.Name);// nested array property an UntypedNode
    }
    [Fact]
    public void Object_Arrays_are_supported()
    {
        OpenApiSchema userSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                },
                {
                    "displayName", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["users/{id}"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = JsonSchemaType.Object,
                                                Properties = new Dictionary<string, IOpenApiSchema> {
                                                    {
                                                        "value", new OpenApiSchema {
                                                            Type = JsonSchemaType.Array,
                                                            Items = new OpenApiSchemaReference("microsoft.graph.user")
                                                        }
                                                    },
                                                    {
                                                        "unknown", new OpenApiSchema {
                                                            Type = JsonSchemaType.Array,
                                                            Items = new OpenApiSchema {
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
        };
        document.AddComponent("microsoft.graph.user", userSchema);
        document.SetReferenceHostDocument();
        CountLogger<ReQuestyBuilder> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? userClass = codeModel.FindNamespaceByName("ApiSdk.models")?.FindChildByName<CodeClass>("user");
        Assert.NotNull(userClass);
        CodeClass? userResponseClass = codeModel.FindNamespaceByName("ApiSdk.users.item")?.FindChildByName<CodeClass>("UsersGetResponse", false);
        Assert.NotNull(userResponseClass);
        Assert.Equal("UsersGetResponse", userResponseClass.Name, StringComparer.Ordinal); //checking for casing
        Assert.NotNull(userResponseClass);
        CodeProperty? valueProp = userResponseClass.FindChildByName<CodeProperty>("value", false);
        Assert.NotNull(valueProp);
        CodeProperty? unknownProp = userResponseClass.FindChildByName<CodeProperty>("unknown", false);
        Assert.NotNull(unknownProp);
        Assert.Equal(ReQuestyBuilder.UntypedNodeName, unknownProp.Type.Name);// left out property is an UntypedNode
    }
    [Fact]
    public void TextPlainEndpointsAreSupported()
    {
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["users/$count"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["text/plain"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = JsonSchemaType.Number,
                                                Format = "int32",
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
        };
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? requestBuilderClass = codeModel.FindChildByName<CodeClass>("CountRequestBuilder");
        Assert.NotNull(requestBuilderClass);
        CodeMethod? executorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executorMethod);
        CodeType? methodReturnType = executorMethod.ReturnType as CodeType;
        Assert.NotNull(methodReturnType);
        Assert.Equal("integer", methodReturnType.Name);
    }
    [Fact]
    public void Supports_Path_Parameters()
    {
        OpenApiSchema resourceActionSchema = new()
        {
            Type = JsonSchemaType.Object,
            Title = "resourceAction",
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "allowedResourceActions", new OpenApiSchema {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema {
                            Type = JsonSchemaType.String
                        }
                    }
                },
                {
                    "notAllowedResourceActions", new OpenApiSchema {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema {
                            Type = JsonSchemaType.String
                        }
                    }
                }
            },
        };
        OpenApiSchema permissionSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "resourceActions", new OpenApiSchema {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema {
                            AnyOf = [
                                new OpenApiSchemaReference("microsoft.graph.resourceAction"),
                            ]
                        }
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["/deviceManagement/microsoft.graph.getEffectivePermissions(scope='{scope}')"] = new OpenApiPathItem
                {
                    Parameters = [
                        new OpenApiParameter
                        {
                            Name = "scope",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        }
                    ],
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = JsonSchemaType.Array,
                                                Items = new OpenApiSchema
                                                {
                                                    AnyOf = [
                                                        new OpenApiSchemaReference("microsoft.graph.rolePermission"),
                                                    ]
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
        };
        document.AddComponent("microsoft.graph.resourceAction", resourceActionSchema);
        document.AddComponent("microsoft.graph.rolePermission", permissionSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? deviceManagementNS = codeModel.FindNamespaceByName("ApiSdk.deviceManagement");
        Assert.NotNull(deviceManagementNS);
        CodeClass? deviceManagementRequestBuilder = deviceManagementNS.FindChildByName<CodeClass>("DeviceManagementRequestBuilder", false);
        Assert.NotNull(deviceManagementRequestBuilder);
        CodeMethod? getEffectivePermissionsMethod = deviceManagementRequestBuilder.FindChildByName<CodeMethod>("microsoftGraphGetEffectivePermissionsWithScope", false);
        Assert.NotNull(getEffectivePermissionsMethod);
        Assert.Single(getEffectivePermissionsMethod.Parameters);
        CodeNamespace? getEffectivePermissionsNS = codeModel.FindNamespaceByName("ApiSdk.deviceManagement.microsoftGraphGetEffectivePermissionsWithScope");
        Assert.NotNull(getEffectivePermissionsNS);
        CodeClass? getEffectivePermissionsRequestBuilder = getEffectivePermissionsNS.FindChildByName<CodeClass>("microsoftGraphGetEffectivePermissionsWithScopeRequestBuilder", false);
        Assert.NotNull(getEffectivePermissionsRequestBuilder);
        CodeMethod? constructorMethod = getEffectivePermissionsRequestBuilder.FindChildByName<CodeMethod>("constructor", false);
        Assert.NotNull(constructorMethod);
        Assert.Single(constructorMethod.Parameters, static x => x.IsOfKind(CodeParameterKind.Path));
    }
    [Fact]
    public void Inline_Property_Inheritance_Is_Supported()
    {
        OpenApiSchema resourceSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "info", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["resource/{id}"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = JsonSchemaType.Object,
                                                Properties = new Dictionary<string, IOpenApiSchema> {
                                                    {
                                                        "derivedResource", new OpenApiSchema {
                                                            Type = JsonSchemaType.Object,
                                                            Properties = new Dictionary<string, IOpenApiSchema> {
                                                                {
                                                                    "info2", new OpenApiSchema {
                                                                        Type = JsonSchemaType.Object,
                                                                        Properties = new Dictionary<string, IOpenApiSchema> {
                                                                            {
                                                                                "title", new OpenApiSchema {
                                                                                    Type = JsonSchemaType.String,
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            },
                                                            AllOf = [
                                                                new OpenApiSchemaReference("resource"),
                                                            ]
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
        };
        document.AddComponent("resource", resourceSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        builder.SetOpenApiDocument(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? resourceClass = codeModel.FindNamespaceByName("ApiSdk.models")?.FindChildByName<CodeClass>("resource");
        CodeNamespace? itemsNS = codeModel.FindNamespaceByName("ApiSdk.resource.item");
        Assert.NotNull(itemsNS);
        CodeClass? responseClass = itemsNS.FindChildByName<CodeClass>("ResourceGetResponse");
        CodeClass? derivedResourceClass = itemsNS.FindChildByName<CodeClass>("ResourceGetResponse_derivedResource");
        CodeClass? derivedResourceInfoClass = itemsNS.FindChildByName<CodeClass>("ResourceGetResponse_derivedResource_info2");
        Assert.NotNull(resourceClass);
        Assert.NotNull(derivedResourceClass);
        Assert.NotNull(derivedResourceClass.StartBlock.Inherits);
        Assert.Equal(derivedResourceClass.StartBlock.Inherits.TypeDefinition, resourceClass);
        Assert.NotNull(derivedResourceInfoClass);
        Assert.NotNull(responseClass);
    }

    [Fact]
    public void Inline_Property_Inheritance_Is_Supported2()
    {
        OpenApiSchema resourceSchema = new()
        {
            Type = JsonSchemaType.Object,
        };

        Dictionary<string, IOpenApiSchema> properties = new()
        {
            { "info", new OpenApiSchema { Type = JsonSchemaType.String, } },
            { "derivedResource", new OpenApiSchema { AllOf = [new OpenApiSchemaReference("resource"),] } },
        };

        resourceSchema.Properties = properties;

        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["resource/{id}"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                AllOf =
                                                [
                                                    new OpenApiSchemaReference("resource"),
                                                ]
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
        };
        document.AddComponent("resource", resourceSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        builder.SetOpenApiDocument(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? resourceClass = codeModel.FindNamespaceByName("ApiSdk.models")?.FindChildByName<CodeClass>("resource");
        CodeNamespace? itemsNS = codeModel.FindNamespaceByName("ApiSdk.resource.item");
        Assert.NotNull(itemsNS);
        CodeClass? responseClass = itemsNS.FindChildByName<CodeClass>("ResourceResponse");


        Assert.NotNull(resourceClass);
        Assert.Null(responseClass);
    }
    [Fact]
    public void MapsTime()
    {
        OpenApiUrlTreeNode node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = new()
            {
                [NetHttpMethod.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.Object,
                                        Properties = new Dictionary<string, IOpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    Type = JsonSchemaType.String,
                                                    Format = "time"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeProperty? progressProp = codeModel.FindChildByName<CodeProperty>("progress");
        Assert.NotNull(progressProp);
        Assert.Equal("TimeOnly", progressProp.Type.Name);
    }
    [Fact]
    public void MapsDate()
    {
        OpenApiUrlTreeNode node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = new()
            {
                [NetHttpMethod.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.Object,
                                        Properties = new Dictionary<string, IOpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    Type = JsonSchemaType.String,
                                                    Format = "date"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeProperty? progressProp = codeModel.FindChildByName<CodeProperty>("progress");
        Assert.NotNull(progressProp);
        Assert.Equal("DateOnly", progressProp.Type.Name);
    }
    [Fact]
    public void MapsDuration()
    {
        OpenApiUrlTreeNode node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = new()
            {
                [NetHttpMethod.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.Object,
                                        Properties = new Dictionary<string, IOpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    Type = JsonSchemaType.String,
                                                    Format = "duration"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeProperty? progressProp = codeModel.FindChildByName<CodeProperty>("progress");
        Assert.NotNull(progressProp);
        Assert.Equal("TimeSpan", progressProp.Type.Name);
    }
    [Fact]
    public void AddsErrorMapping()
    {
        OpenApiUrlTreeNode node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = new()
            {
                [NetHttpMethod.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.Object,
                                        Properties = new Dictionary<string, IOpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    Type = JsonSchemaType.String,
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["4XX"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.Object,
                                        Properties = new Dictionary<string, IOpenApiSchema> {
                                            {
                                                "errorId", new OpenApiSchema{
                                                    Type = JsonSchemaType.String,
                                                    Extensions = new Dictionary<string, IOpenApiExtension>
                                                    {
                                                        { OpenApiPrimaryErrorMessageExtension.Name,
                                                                new OpenApiPrimaryErrorMessageExtension {
                                                                    IsPrimaryErrorMessage = true
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["5XX"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.Object,
                                        Properties = new Dictionary<string, IOpenApiSchema> {
                                            {
                                                "serviceErrorId", new OpenApiSchema{
                                                    Type = JsonSchemaType.String,
                                                    Extensions = new Dictionary<string, IOpenApiExtension>
                                                    {
                                                        { OpenApiPrimaryErrorMessageExtension.Name,
                                                                new OpenApiPrimaryErrorMessageExtension {
                                                                    IsPrimaryErrorMessage = true
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["402"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String
                                    }
                                }
                            }
                        },
                        ["401"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.Object,
                                        Properties = new Dictionary<string, IOpenApiSchema> {
                                            {
                                                "authenticationRealm", new OpenApiSchema{
                                                    Type = JsonSchemaType.String,
                                                    Extensions = new Dictionary<string, IOpenApiExtension>
                                                    {
                                                        { OpenApiPrimaryErrorMessageExtension.Name,
                                                                new OpenApiPrimaryErrorMessageExtension {
                                                                    IsPrimaryErrorMessage = true
                                                            }
                                                        }
                                                    }
                                                }
                                            },
                                            {
                                                "authenticationCode", new OpenApiSchema{
                                                    Type = JsonSchemaType.String,
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, "default");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeMethod? executorMethod = codeModel.FindChildByName<CodeMethod>("get");
        Assert.NotNull(executorMethod);
        Assert.NotEmpty(executorMethod.ErrorMappings);
        HashSet<string> keys = executorMethod.ErrorMappings.Select(x => x.Key).ToHashSet();
        Assert.Contains("4XX", keys);
        Assert.Contains("401", keys);
        Assert.DoesNotContain("402", keys);
        Assert.Contains("5XX", keys);
        CodeClass? errorType401 = codeModel.FindChildByName<CodeClass>("tasks401Error");
        Assert.NotNull(errorType401);
        Assert.True(errorType401.IsErrorDefinition);
        CodeProperty? error401Property = errorType401.FindChildByName<CodeProperty>("authenticationCode", false);
        Assert.NotNull(error401Property);
        Assert.False(error401Property.IsPrimaryErrorMessage);
        CodeProperty? errorType401MainProperty = errorType401.FindChildByName<CodeProperty>("authenticationRealm", false);
        Assert.NotNull(errorType401MainProperty);
        Assert.True(errorType401MainProperty.IsPrimaryErrorMessage);
        CodeClass? errorType4XX = codeModel.FindChildByName<CodeClass>("tasks4XXError");
        Assert.NotNull(errorType4XX);
        Assert.True(errorType4XX.IsErrorDefinition);
        CodeProperty? errorType4XXProperty = errorType4XX.FindChildByName<CodeProperty>("errorId", false);
        Assert.NotNull(errorType4XXProperty);
        Assert.True(errorType4XXProperty.IsPrimaryErrorMessage);
        CodeClass? errorType5XX = codeModel.FindChildByName<CodeClass>("tasks5XXError");
        Assert.NotNull(errorType5XX);
        Assert.True(errorType5XX.IsErrorDefinition);
        CodeProperty? errorType5XXProperty = errorType5XX.FindChildByName<CodeProperty>("serviceErrorId", false);
        Assert.NotNull(errorType5XXProperty);
        Assert.True(errorType5XXProperty.IsPrimaryErrorMessage);
    }
    [Fact]
    public void IgnoresErrorCodesWithNoSchema()
    {
        OpenApiUrlTreeNode node = OpenApiUrlTreeNode.Create();
        node.Attach("tasks", new OpenApiPathItem
        {
            Operations = new()
            {
                [NetHttpMethod.Get] = new OpenApiOperation
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.Object,
                                        Properties = new Dictionary<string, IOpenApiSchema> {
                                            {
                                                "progress", new OpenApiSchema{
                                                    Type = JsonSchemaType.String,
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["4XX"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                            }
                        },
                        ["5XX"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                            }
                        },
                        ["401"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>()
                            {
                                ["application/json"] = new OpenApiMediaType()
                            }
                        }
                    }
                }
            }
        }, "default");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeMethod? executorMethod = codeModel.FindChildByName<CodeMethod>("get");
        Assert.NotNull(executorMethod);
        Assert.Empty(executorMethod.ErrorMappings);
    }
    [Fact]
    public void DoesntAddSuffixesToErrorTypesWhenComponents()
    {
        OpenApiSchema errorSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "errorId", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        OpenApiResponse errorResponse = new()
        {
            Content = new Dictionary<string, IOpenApiMediaType>()
            {
                ["application/json"] = new OpenApiMediaType()
                {
                    Schema = new OpenApiSchemaReference("microsoft.graph.error")
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["tasks"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = JsonSchemaType.Object,
                                                Properties = new Dictionary<string, IOpenApiSchema> {
                                                    {
                                                        "progress", new OpenApiSchema{
                                                            Type = JsonSchemaType.String,
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                ["4XX"] = new OpenApiResponseReference("microsoft.graph.error"),
                                ["5XX"] = new OpenApiResponseReference("microsoft.graph.error"),
                                ["401"] = new OpenApiResponseReference("microsoft.graph.error")
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("microsoft.graph.error", errorSchema);
        document.AddComponent("microsoft.graph.error", errorResponse);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        builder.SetOpenApiDocument(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeMethod? executorMethod = codeModel.FindChildByName<CodeMethod>("get");
        Assert.NotNull(executorMethod);
        Assert.NotEmpty(executorMethod.ErrorMappings);
        HashSet<string> keys = executorMethod.ErrorMappings.Select(x => x.Key).ToHashSet();
        Assert.Contains("4XX", keys);
        Assert.Contains("401", keys);
        Assert.Contains("5XX", keys);
        CodeClass? errorType = codeModel.FindChildByName<CodeClass>("Error");
        Assert.NotNull(errorType);
        Assert.True(errorType.IsErrorDefinition);
        Assert.NotNull(errorType.FindChildByName<CodeProperty>("errorId"));

        Assert.Null(codeModel.FindChildByName<CodeClass>("tasks401Error"));
        Assert.Null(codeModel.FindChildByName<CodeClass>("tasks4XXError"));
        Assert.Null(codeModel.FindChildByName<CodeClass>("tasks5XXError"));
    }
    [Fact]
    public void UsesDefaultAs4XXAnd5XXWhenAbsent()
    {
        OpenApiSchema errorSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "errorId", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        OpenApiResponse errorResponse = new()
        {
            Content = new Dictionary<string, IOpenApiMediaType>()
            {
                ["application/json"] = new OpenApiMediaType()
                {
                    Schema = new OpenApiSchemaReference("microsoft.graph.error")
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["tasks"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = JsonSchemaType.Object,
                                                Properties = new Dictionary<string, IOpenApiSchema> {
                                                    {
                                                        "progress", new OpenApiSchema{
                                                            Type = JsonSchemaType.String,
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                ["default"] = new OpenApiResponseReference("microsoft.graph.error"),
                                ["401"] = new OpenApiResponseReference("microsoft.graph.error")
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("microsoft.graph.error", errorSchema);
        document.AddComponent("microsoft.graph.error", errorResponse);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        builder.SetOpenApiDocument(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeMethod? executorMethod = codeModel.FindChildByName<CodeMethod>("get");
        Assert.NotNull(executorMethod);
        Assert.NotEmpty(executorMethod.ErrorMappings);
        HashSet<string> keys = executorMethod.ErrorMappings.Select(static x => x.Key).ToHashSet();
        Assert.Contains("4XX", keys);
        Assert.Contains("401", keys);
        Assert.Contains("5XX", keys);
        CodeClass? errorType = codeModel.FindChildByName<CodeClass>("Error");
        Assert.NotNull(errorType);
        Assert.True(errorType.IsErrorDefinition);
        Assert.NotNull(errorType.FindChildByName<CodeProperty>("errorId"));

        Assert.Null(codeModel.FindChildByName<CodeClass>("tasks401Error"));
        Assert.Null(codeModel.FindChildByName<CodeClass>("tasks4XXError"));
        Assert.Null(codeModel.FindChildByName<CodeClass>("tasks5XXError"));
    }
    [Fact]
    public void DoesntAddPropertyHolderOnNonAdditionalModels()
    {
        OpenApiSchema weatherForecastSchema = new()
        {
            Type = JsonSchemaType.Object,
            AdditionalPropertiesAllowed = false,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "date", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                        Format = "date-time"
                    }
                },
                {
                    "temperature", new OpenApiSchema {
                        Type = JsonSchemaType.Integer,
                        Format = "int32"
                    }
                }
            },
        };
        OpenApiResponse weatherForecastResponse = new()
        {
            Content = new Dictionary<string, IOpenApiMediaType>()
            {
                ["application/json"] = new OpenApiMediaType()
                {
                    Schema = new OpenApiSchemaReference("weatherForecast")
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["weatherforecast"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponseReference("weatherForecastResponse")
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("weatherForecast", weatherForecastSchema);
        document.AddComponent("weatherForecastResponse", weatherForecastResponse);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        builder.SetOpenApiDocument(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? weatherType = codeModel.FindChildByName<CodeClass>("WeatherForecast");
        Assert.NotNull(weatherType);
        Assert.DoesNotContain(weatherType.StartBlock.Implements, x => x.Name.Equals("IAdditionalDataHolder", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(weatherType.Properties, x => x.IsOfKind(CodePropertyKind.AdditionalData));
    }
    [Fact]
    public void AddPropertyHolderOnAdditionalPropertiesSchema()
    {
        OpenApiSchema weatherForecastSchema = new()
        {
            Type = JsonSchemaType.Object,
            AdditionalPropertiesAllowed = false,
            AdditionalProperties = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
            },
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "date", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                        Format = "date-time"
                    }
                },
                {
                    "temperature", new OpenApiSchema {
                        Type = JsonSchemaType.Integer,
                        Format = "int32"
                    }
                }
            },
        };
        OpenApiResponse weatherForecastResponse = new()
        {
            Content = new Dictionary<string, IOpenApiMediaType>()
            {
                ["application/json"] = new OpenApiMediaType()
                {
                    Schema = new OpenApiSchemaReference("weatherForecast")
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["weatherforecast"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponseReference("weatherForecastResponse")
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("weatherForecast", weatherForecastSchema);
        document.AddComponent("weatherForecastResponse", weatherForecastResponse);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        builder.SetOpenApiDocument(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? weatherType = codeModel.FindChildByName<CodeClass>("WeatherForecast");
        Assert.NotNull(weatherType);
        Assert.Contains(weatherType.StartBlock.Implements, x => x.Name.Equals("IAdditionalDataHolder", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(weatherType.Properties, x => x.IsOfKind(CodePropertyKind.AdditionalData));
    }
    [Fact]
    public void SquishesLonelyNullables()
    {
        OpenApiSchema uploadSessionSchema = new()
        {
            Type = JsonSchemaType.Object,
            AdditionalPropertiesAllowed = false,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "date", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                        Format = "date-time"
                    }
                },
                {
                    "temperature", new OpenApiSchema {
                        Type = JsonSchemaType.Integer,
                        Format = "int32"
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["createUploadSession"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                AnyOf = [
                                                    new OpenApiSchemaReference("microsoft.graph.uploadSession")
                                                ]
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("microsoft.graph.uploadSession", uploadSessionSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        builder.SetOpenApiDocument(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? responseClass = codeModel.FindChildByName<CodeClass>("CreateUploadSessionResponse");
        Assert.Null(responseClass);
        CodeClass? sessionClass = codeModel.FindChildByName<CodeClass>("UploadSession");
        Assert.NotNull(sessionClass);
        CodeClass? requestBuilderClass = codeModel.FindChildByName<CodeClass>("createUploadSessionRequestBuilder");
        Assert.NotNull(requestBuilderClass);
        CodeMethod? executorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executorMethod);
        Assert.True(executorMethod.ReturnType is CodeType); // not union
        Assert.Null(codeModel.FindChildByName<CodeClass>("createUploadSessionResponseMember1"));
    }
    [Fact]
    public void SquishesLonelyNullablesBothAnyOf()
    {
        OpenApiSchema uploadSessionSchema = new()
        {
            Type = JsonSchemaType.Object,
            AdditionalPropertiesAllowed = false,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "date", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                        Format = "date-time"
                    }
                },
                {
                    "temperature", new OpenApiSchema {
                        Type = JsonSchemaType.Integer,
                        Format = "int32"
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["createUploadSession"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                AnyOf = [
                                                    new OpenApiSchemaReference("microsoft.graph.uploadSession"),
                                                    new OpenApiSchema {
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("microsoft.graph.uploadSession", uploadSessionSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        builder.SetOpenApiDocument(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? responseClass = codeModel.FindChildByName<CodeClass>("CreateUploadSessionResponse");
        Assert.Null(responseClass);
        CodeClass? sessionClass = codeModel.FindChildByName<CodeClass>("UploadSession");
        Assert.NotNull(sessionClass);
        CodeClass? requestBuilderClass = codeModel.FindChildByName<CodeClass>("createUploadSessionRequestBuilder");
        Assert.NotNull(requestBuilderClass);
        CodeMethod? executorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executorMethod);
        Assert.True(executorMethod.ReturnType is CodeType); // not union
        Assert.Null(codeModel.FindChildByName<CodeClass>("createUploadSessionResponseMember1"));
    }
    [Fact]
    public void SupportsArraysInComposedTypes()
    {
        OpenApiSchema anyOfSchema = new()
        {
            Type = JsonSchemaType.Object,
            AdditionalPropertiesAllowed = false,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "date", new OpenApiSchema {
                        AnyOf = [
                            new OpenApiSchema {
                                Type = JsonSchemaType.String,
                            },
                            new OpenApiSchema {
                                Type = JsonSchemaType.Array,
                                Items = new OpenApiSchema {
                                    Type = JsonSchemaType.String,
                                },
                            },
                        ]
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["createUploadSession"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("anyOfNullable")
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("anyOfNullable", anyOfSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        builder.SetOpenApiDocument(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? anyOfClass = codeModel.FindChildByName<CodeClass>("anyOfNullable");
        Assert.NotNull(anyOfClass);
        CodeProperty? dateProperty = anyOfClass.FindChildByName<CodeProperty>("date", false);
        Assert.NotNull(dateProperty);
        if (dateProperty.Type is not CodeIntersectionType unionType)
        {
            Assert.Fail("Date property type is not a union type");
        }
        else
        {
            Assert.Equal(2, unionType.Types.Count());
            Assert.Contains(unionType.Types, x => x.Name.Equals("string", StringComparison.OrdinalIgnoreCase) && x.CollectionKind is CodeTypeBase.CodeTypeCollectionKind.None);
            Assert.Contains(unionType.Types, x => x.Name.Equals("string", StringComparison.OrdinalIgnoreCase) && x.CollectionKind is CodeTypeBase.CodeTypeCollectionKind.Complex);
        }
    }
    [Fact]
    public void SupportsNullableAnyOf()
    {
        OpenApiSchema anyOfSchema = new()
        {
            Type = JsonSchemaType.Object,
            AdditionalPropertiesAllowed = false,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "date", new OpenApiSchema {
                        AnyOf = [
                            new OpenApiSchema {
                                Type = JsonSchemaType.String,
                            },
                            new OpenApiSchema {
                                Type = JsonSchemaType.Number,
                                Format = "int64",
                            }
                        ]
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["createUploadSession"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("anyOfNullable")
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("anyOfNullable", anyOfSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        builder.SetOpenApiDocument(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? anyOfClass = codeModel.FindChildByName<CodeClass>("anyOfNullable");
        Assert.NotNull(anyOfClass);
        CodeProperty? dateProperty = anyOfClass.FindChildByName<CodeProperty>("date", false);
        Assert.NotNull(dateProperty);
        if (dateProperty.Type is not CodeIntersectionType unionType)
        {
            Assert.Fail("Date property type is not a union type");
        }
        else
        {
            Assert.Equal(2, unionType.Types.Count());
            Assert.Contains(unionType.Types, x => x.Name.Equals("string", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(unionType.Types, x => x.Name.Equals("int64", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void AddsDiscriminatorMappings()
    {
        OpenApiSchema entitySchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                },
                {
                    "@odata.type", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                        Default = "#microsoft.graph.entity"
                    }
                }
            },
            Required = new HashSet<string> {
                "@odata.type"
            },
            Discriminator = new()
            {
                PropertyName = "@odata.type",
                Mapping = new Dictionary<string, OpenApiSchemaReference> {
                    {
                        "#microsoft.graph.directoryObject", new OpenApiSchemaReference("microsoft.graph.directoryObject")
                    }
                }
            },
        };
        OpenApiSchema directoryObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "tenant", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                },
                {   "@odata.type", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                        Default = "#microsoft.graph.directoryObject"
                    }
                }
            },
            AllOf = [
                new OpenApiSchemaReference("microsoft.graph.entity")
            ],
        };
        OpenApiResponse directoryObjects = new()
        {
            Content = new Dictionary<string, IOpenApiMediaType>()
            {
                ["application/json"] = new OpenApiMediaType()
                {
                    Schema = new OpenApiSchemaReference("microsoft.graph.entity")
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["objects"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponseReference("microsoft.graph.directoryObjects"),
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("microsoft.graph.entity", entitySchema);
        document.AddComponent("microsoft.graph.directoryObject", directoryObjectSchema);
        document.AddComponent("microsoft.graph.directoryObjects", directoryObjects);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? entityClass = codeModel.FindChildByName<CodeClass>("entity");
        CodeClass? directoryObjectClass = codeModel.FindChildByName<CodeClass>("directoryObject");
        Assert.NotNull(entityClass);
        CodeMethod? factoryMethod = entityClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(factoryMethod);
        Assert.Equal("@odata.type", entityClass.DiscriminatorInformation.DiscriminatorPropertyName);
        Assert.Single(entityClass.DiscriminatorInformation.DiscriminatorMappings);
        Assert.NotNull(directoryObjectClass);
        CodeMethod? doFactoryMethod = directoryObjectClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(doFactoryMethod);
        Assert.Empty(directoryObjectClass.DiscriminatorInformation.DiscriminatorMappings);
        if (entityClass.DiscriminatorInformation?.GetDiscriminatorMappingValue("#microsoft.graph.directoryObject") is not CodeType castType)
        {
            throw new InvalidOperationException("Discriminator mapping value is not a CodeType");
        }

        Assert.NotNull(castType.TypeDefinition);
        Assert.Equal(directoryObjectClass, castType.TypeDefinition);
        CodeProperty doTypeProperty = directoryObjectClass.Properties.First(static x => x.Name.Equals("ODataType", StringComparison.OrdinalIgnoreCase));
        Assert.True(doTypeProperty.ExistsInBaseType);
        Assert.Equal("\"#microsoft.graph.directoryObject\"", doTypeProperty.DefaultValue);
    }
    [Fact]
    public void DoesntAddDiscriminatorMappingsOfNonDerivedTypes()
    {
        OpenApiSchema entitySchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                },
                {
                    "@odata.type", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                        Default = "#microsoft.graph.entity"
                    }
                }
            },
            Required = new HashSet<string> {
                "@odata.type"
            },
            Discriminator = new()
            {
                PropertyName = "@odata.type",
                Mapping = new Dictionary<string, OpenApiSchemaReference> {
                    {
                        "#microsoft.graph.directoryObject", new OpenApiSchemaReference("microsoft.graph.directoryObject")
                    },
                    {
                        "#microsoft.graph.file", new OpenApiSchemaReference("microsoft.graph.file")
                    }
                }
            },
        };
        OpenApiSchema directoryObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "tenant", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                },
                {   "@odata.type", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                        Default = "#microsoft.graph.directoryObject"
                    }
                }
            },
            AllOf = [
                new OpenApiSchemaReference("microsoft.graph.entity")
            ],
        };
        OpenApiSchema fileSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "tenant", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                },
                {   "@odata.type", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                        Default = "#microsoft.graph.file"
                    }
                }
            },
        };
        OpenApiResponse directoryObjects = new()
        {
            Content = new Dictionary<string, IOpenApiMediaType>()
            {
                ["application/json"] = new OpenApiMediaType()
                {
                    Schema = new OpenApiSchemaReference("microsoft.graph.entity")
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths()
            {
                ["objects"] = new OpenApiPathItem()
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation()
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponseReference("microsoft.graph.directoryObjects"),
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("microsoft.graph.entity", entitySchema);
        document.AddComponent("microsoft.graph.directoryObject", directoryObjectSchema);
        document.AddComponent("microsoft.graph.file", fileSchema);
        document.AddComponent("microsoft.graph.directoryObjects", directoryObjects);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? entityClass = codeModel.FindChildByName<CodeClass>("entity", true);
        Assert.NotNull(entityClass);
        Assert.NotNull(codeModel.FindChildByName<CodeClass>("directoryObject", true));
        CodeMethod? factoryMethod = entityClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(factoryMethod);
        Assert.Equal("@odata.type", entityClass.DiscriminatorInformation.DiscriminatorPropertyName);
        Assert.Single(entityClass.DiscriminatorInformation.DiscriminatorMappings);
    }
    [Fact]
    public async Task AddsDiscriminatorMappingsOneOfImplicitAsync()
    {
        OpenApiSchema entitySchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                },
                {
                    "@odata.type", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                        Default = "microsoft.graph.entity"
                    }
                }
            },
            Required = new HashSet<string> {
                "@odata.type"
            },
            Discriminator = new()
            {
                PropertyName = "@odata.type",
            },
        };
        OpenApiSchema directoryObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "tenant", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                },
                {   "@odata.type", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                        Default = "microsoft.graph.directoryObject"
                    }
                }
            },
            Required = new HashSet<string> {
                "@odata.type"
            },
        };
        OpenApiSchema directoryObjectsResponse = new()
        {
            Type = JsonSchemaType.Object,
            OneOf = [
                new OpenApiSchemaReference("microsoft.graph.entity"),
                new OpenApiSchemaReference("microsoft.graph.directoryObject")
            ],
        };
        OpenApiResponse directoryObjects = new()
        {
            Content = new Dictionary<string, IOpenApiMediaType>()
            {
                ["application/json"] = new OpenApiMediaType()
                {
                    Schema = new OpenApiSchemaReference("microsoft.graph.directoryObjects")
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["objects"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponseReference("microsoft.graph.directoryObjects"),
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("microsoft.graph.entity", entitySchema);
        document.AddComponent("microsoft.graph.directoryObject", directoryObjectSchema);
        document.AddComponent("microsoft.graph.directoryObjects", directoryObjectsResponse);
        document.AddComponent("microsoft.graph.directoryObjects", directoryObjects);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        GenerationConfiguration config = new() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" };
        ReQuestyBuilder builder = new(mockLogger.Object, config, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        await builder.ApplyLanguageRefinementAsync(config, codeModel, CancellationToken.None);
        CodeClass? entityClass = codeModel.FindChildByName<CodeClass>("entity");
        CodeClass? directoryObjectsClass = codeModel.FindChildByName<CodeClass>("directoryObjects");
        Assert.NotNull(entityClass);
        Assert.NotNull(directoryObjectsClass);
        CodeMethod? factoryMethod = entityClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(factoryMethod);
        Assert.Equal("@odata.type", entityClass.DiscriminatorInformation.DiscriminatorPropertyName);
        Assert.Empty(entityClass.DiscriminatorInformation.DiscriminatorMappings);
        CodeMethod? doFactoryMethod = directoryObjectsClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(doFactoryMethod);
        Assert.Equal(2, directoryObjectsClass.DiscriminatorInformation.DiscriminatorMappings.Count());
    }
    [Fact]
    public async Task AddsDiscriminatorMappingsAllOfImplicitAsync()
    {
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["objects"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponseReference("microsoft.graph.directoryObjects"),
                            }
                        }
                    }
                }
            },
        };
        OpenApiSchema entitySchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                },
                {
                    "@odata.type", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                        Default = "#microsoft.graph.entity"
                    }
                }
            },
            Required = new HashSet<string> {
                "@odata.type"
            },
            Discriminator = new()
            {
                PropertyName = "@odata.type",
            },
        };
        OpenApiSchema directoryObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            AllOf = [
                new OpenApiSchemaReference("microsoft.graph.entity", document),
                new OpenApiSchema {
                    Properties = new Dictionary<string, IOpenApiSchema> {
                        {
                            "tenant", new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        },
                        {   "@odata.type", new OpenApiSchema {
                                Type = JsonSchemaType.String,
                                Default = "microsoft.graph.directoryObject"
                            }
                        }
                    },
                    Required = new HashSet<string> {
                        "@odata.type"
                    }
                }
            ],
        };
        OpenApiSchema userSchema = new()
        {
            Type = JsonSchemaType.Object,
            AllOf = [
                new OpenApiSchemaReference("microsoft.graph.directoryObject", document),
                new OpenApiSchema {
                    Properties = new Dictionary<string, IOpenApiSchema> {
                        {
                            "firstName", new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        },
                        {   "@odata.type", new OpenApiSchema {
                                Type = JsonSchemaType.String,
                                Default = "microsoft.graph.firstName"
                            }
                        }
                    },
                    Required = new HashSet<string> {
                        "@odata.type"
                    }
                }
            ],
        };
        OpenApiResponse directoryObjects = new()
        {
            Content = new Dictionary<string, IOpenApiMediaType>()
            {
                ["application/json"] = new OpenApiMediaType()
                {
                    Schema = new OpenApiSchemaReference("microsoft.graph.directoryObject", document)
                }
            },
        };

        document.AddComponent("microsoft.graph.entity", entitySchema);
        document.AddComponent("microsoft.graph.directoryObject", directoryObjectSchema);
        document.AddComponent("microsoft.graph.user", userSchema);
        document.AddComponent("microsoft.graph.directoryObjects", directoryObjects);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        GenerationConfiguration config = new() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" };
        ReQuestyBuilder builder = new(mockLogger.Object, config, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        await builder.ApplyLanguageRefinementAsync(config, codeModel, CancellationToken.None);
        CodeClass? entityClass = codeModel.FindChildByName<CodeClass>("entity");
        CodeClass? directoryObjectClass = codeModel.FindChildByName<CodeClass>("directoryObject");
        CodeClass? userClass = codeModel.FindChildByName<CodeClass>("user");
        mockLogger.Verify(logger => logger.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Warning),
                It.Is<EventId>(eventId => eventId.Id == 0),
                It.Is<It.IsAnyType>((@object, @type) => @object.ToString()!.Contains(" is not inherited from ", StringComparison.OrdinalIgnoreCase) && @type.Name == "FormattedLogValues"),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        Assert.NotNull(entityClass);
        Assert.NotNull(directoryObjectClass);
        Assert.NotNull(userClass);
        CodeMethod? factoryMethod = entityClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(factoryMethod);
        Assert.Equal("@odata.type", entityClass.DiscriminatorInformation.DiscriminatorPropertyName);
        Assert.Equal(2, entityClass.DiscriminatorInformation.DiscriminatorMappings.Count());
        Assert.Contains("microsoft.graph.directoryObject", entityClass.DiscriminatorInformation.DiscriminatorMappings.Select(static x => x.Key));
        Assert.Contains("microsoft.graph.user", entityClass.DiscriminatorInformation.DiscriminatorMappings.Select(static x => x.Key));
        CodeMethod? doFactoryMethod = directoryObjectClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(doFactoryMethod);
        Assert.Single(directoryObjectClass.DiscriminatorInformation.DiscriminatorMappings);
        Assert.DoesNotContain(directoryObjectClass.Properties, static x => x.Name.Equals("id", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("microsoft.graph.user", directoryObjectClass.DiscriminatorInformation.DiscriminatorMappings.Select(static x => x.Key));
        Assert.Empty(userClass.DiscriminatorInformation.DiscriminatorMappings);
    }

    [Fact]
    public async Task AddsDiscriminatorMappingsAllOfImplicitWithParentHavingMappingsWhileChildDoesNotAsync()
    {
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["objects"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponseReference("microsoft.graph.directoryObjects"),
                            }
                        }
                    }
                }
            },
        };
        OpenApiSchema entitySchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                },
                {
                    "@odata.type", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                        Default = "#microsoft.graph.entity"
                    }
                }
            },
            Required = new HashSet<string> {
                "@odata.type"
            },
            Discriminator = new()
            {
                PropertyName = "@odata.type",
                Mapping = new Dictionary<string, OpenApiSchemaReference>
                {
                    {
                        "microsoft.graph.directoryObject", new OpenApiSchemaReference("microsoft.graph.directoryObject", document)
                    },
                    {
                        "microsoft.graph.user", new OpenApiSchemaReference("microsoft.graph.user", document)
                    }
                }
            },
        };
        OpenApiSchema directoryObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            AllOf = [
                new OpenApiSchemaReference("microsoft.graph.entity", document),
                new OpenApiSchema
                {
                    Properties = new Dictionary<string, IOpenApiSchema> {
                        {
                            "tenant", new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        },
                        {   "@odata.type", new OpenApiSchema {
                                Type = JsonSchemaType.String,
                                Default = "microsoft.graph.directoryObject"
                            }
                        }
                    },
                    Required = new HashSet<string> {
                        "@odata.type"
                    }
                }
            ],
        };
        OpenApiSchema userSchema = new()
        {
            Type = JsonSchemaType.Object,
            AllOf = [
                new OpenApiSchemaReference("microsoft.graph.directoryObject", document),
                new OpenApiSchema
                {
                    Properties = new Dictionary<string, IOpenApiSchema> {
                        {
                            "firstName", new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        },
                        {   "@odata.type", new OpenApiSchema {
                                Type = JsonSchemaType.String,
                                Default = "microsoft.graph.firstName"
                            }
                        }
                    },
                    Required = new HashSet<string> {
                        "@odata.type"
                    }
                }
            ],
        };
        OpenApiResponse directoryObjects = new()
        {
            Content = new Dictionary<string, IOpenApiMediaType>()
            {
                ["application/json"] = new OpenApiMediaType()
                {
                    Schema = new OpenApiSchemaReference("microsoft.graph.directoryObject", document)
                }
            },
        };
        document.AddComponent("microsoft.graph.entity", entitySchema);
        document.AddComponent("microsoft.graph.directoryObject", directoryObjectSchema);
        document.AddComponent("microsoft.graph.user", userSchema);
        document.AddComponent("microsoft.graph.directoryObjects", directoryObjects);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        GenerationConfiguration config = new() { ClientClassName = "Graph", ApiRootUrl = "https://localhost" };
        ReQuestyBuilder builder = new(mockLogger.Object, config, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        await builder.ApplyLanguageRefinementAsync(config, codeModel, CancellationToken.None);
        CodeClass? entityClass = codeModel.FindChildByName<CodeClass>("entity");
        CodeClass? directoryObjectClass = codeModel.FindChildByName<CodeClass>("directoryObject");
        CodeClass? userClass = codeModel.FindChildByName<CodeClass>("user");
        Assert.NotNull(entityClass);
        Assert.NotNull(directoryObjectClass);
        Assert.NotNull(userClass);
        CodeMethod? factoryMethod = entityClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(factoryMethod);
        Assert.Equal("@odata.type", entityClass.DiscriminatorInformation.DiscriminatorPropertyName);
        Assert.Equal(2, entityClass.DiscriminatorInformation.DiscriminatorMappings.Count());
        Assert.Contains("microsoft.graph.directoryObject", entityClass.DiscriminatorInformation.DiscriminatorMappings.Select(static x => x.Key));
        Assert.Contains("microsoft.graph.user", entityClass.DiscriminatorInformation.DiscriminatorMappings.Select(static x => x.Key));
        CodeMethod? doFactoryMethod = directoryObjectClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(doFactoryMethod);
        Assert.Single(directoryObjectClass.DiscriminatorInformation.DiscriminatorMappings);
        Assert.Contains("microsoft.graph.user", directoryObjectClass.DiscriminatorInformation.DiscriminatorMappings.Select(static x => x.Key));
        Assert.Empty(userClass.DiscriminatorInformation.DiscriminatorMappings);
    }
    [Fact]
    public void UnionOfPrimitiveTypesWorks()
    {
        OpenApiSchema simpleObjet = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["unionType"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                OneOf = [
                                                    new OpenApiSchemaReference("subNS.simpleObject"),
                                                    new OpenApiSchema {
                                                        Type = JsonSchemaType.Number
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("subNS.simpleObject", simpleObjet);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.unionType");
        Assert.NotNull(requestBuilderNS);
        CodeClass? requestBuilderClass = requestBuilderNS.FindChildByName<CodeClass>("unionTypeRequestBuilder", false);
        Assert.NotNull(requestBuilderClass);
        CodeMethod? requestExecutorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(requestExecutorMethod);
        CodeUnionType? executorReturnType = requestExecutorMethod.ReturnType as CodeUnionType;
        Assert.NotNull(executorReturnType);
        Assert.Equal(2, executorReturnType.Types.Count());
        HashSet<string> typeNames = executorReturnType.Types.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("simpleObject", typeNames);
        Assert.Contains("double", typeNames);
    }

    [Fact]
    public void UnionOfInlineSchemasWorks()
    {
        OpenApiSchema simpleObjet = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["unionType"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                OneOf = [
                                                    new OpenApiSchemaReference("subNS.simpleObject"),
                                                    new OpenApiSchema {
                                                        Type = JsonSchemaType.Object,
                                                        Properties = new Dictionary<string, IOpenApiSchema> {
                                                            {
                                                                "name", new OpenApiSchema {
                                                                    Type = JsonSchemaType.String
                                                                }
                                                            }
                                                        }
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("subNS.simpleObject", simpleObjet);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.unionType");
        Assert.NotNull(requestBuilderNS);
        CodeClass? requestBuilderClass = requestBuilderNS.FindChildByName<CodeClass>("unionTypeRequestBuilder", false);
        Assert.NotNull(requestBuilderClass);
        CodeMethod? requestExecutorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(requestExecutorMethod);
        CodeUnionType? executorReturnType = requestExecutorMethod.ReturnType as CodeUnionType;
        Assert.NotNull(executorReturnType);
        Assert.Equal(2, executorReturnType.Types.Count());
        HashSet<string> typeNames = executorReturnType.Types.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("simpleObject", typeNames);
        Assert.Contains("unionTypeGetResponseMember1", typeNames);
    }
    [Fact]
    public void IntersectionOfPrimitiveTypesWorks()
    {
        OpenApiSchema simpleObjet = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["unionType"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                AnyOf = [
                                                    new OpenApiSchemaReference("subNS.simpleObject"),
                                                    new OpenApiSchema {
                                                        Type = JsonSchemaType.Number
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("subNS.simpleObject", simpleObjet);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.unionType");
        Assert.NotNull(requestBuilderNS);
        CodeClass? requestBuilderClass = requestBuilderNS.FindChildByName<CodeClass>("unionTypeRequestBuilder", false);
        Assert.NotNull(requestBuilderClass);
        CodeMethod? requestExecutorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(requestExecutorMethod);
        CodeIntersectionType? executorReturnType = requestExecutorMethod.ReturnType as CodeIntersectionType;
        Assert.NotNull(executorReturnType);
        Assert.Equal(2, executorReturnType.Types.Count());
        HashSet<string> typeNames = executorReturnType.Types.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("simpleObject", typeNames);
        Assert.Contains("double", typeNames);
    }
    [Fact]
    public void IntersectionOfInlineSchemasWorks()
    {
        OpenApiSchema simpleObjet = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["unionType"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                AnyOf = [
                                                    new OpenApiSchemaReference("subNS.simpleObject"),
                                                    new OpenApiSchema {
                                                        Type = JsonSchemaType.Object,
                                                        Properties = new Dictionary<string, IOpenApiSchema> {
                                                            {
                                                                "name", new OpenApiSchema {
                                                                    Type = JsonSchemaType.String
                                                                }
                                                            }
                                                        }
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("subNS.simpleObject", simpleObjet);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.unionType");
        Assert.NotNull(requestBuilderNS);
        CodeClass? requestBuilderClass = requestBuilderNS.FindChildByName<CodeClass>("unionTypeRequestBuilder", false);
        Assert.NotNull(requestBuilderClass);
        CodeMethod? requestExecutorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(requestExecutorMethod);
        CodeIntersectionType? executorReturnType = requestExecutorMethod.ReturnType as CodeIntersectionType;
        Assert.NotNull(executorReturnType);
        Assert.Equal(2, executorReturnType.Types.Count());
        HashSet<string> typeNames = executorReturnType.Types.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("simpleObject", typeNames);
        Assert.Contains("unionTypeGetResponseMember1", typeNames);
    }
    [Fact]
    public void InheritedTypeWithInlineSchemaWorks()
    {
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["derivedType"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("subNS.derivedObject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        OpenApiSchema baseObject = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                },
                {
                    "kind", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
            Discriminator = new OpenApiDiscriminator
            {
                PropertyName = "kind",
                Mapping = new Dictionary<string, OpenApiSchemaReference> {
                    {
                        "derivedObject", new OpenApiSchemaReference("subNS.derivedObject", document)
                    }
                }
            },
        };
        OpenApiSchema derivedObject = new()
        {
            Type = JsonSchemaType.Object,
            AllOf = [
                new OpenApiSchemaReference("subNS.baseObject", document),
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema> {
                        {
                            "special", new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        }
                    },
                    Discriminator = new OpenApiDiscriminator
                    {
                        PropertyName = "kind",
                        Mapping = new Dictionary<string, OpenApiSchemaReference> {
                            {
                                "secondLevelDerivedObject", new OpenApiSchemaReference("subNS.secondLevelDerivedObject", document)
                            }
                        }
                    },
                }
            ],
        };
        OpenApiSchema secondLevelDerivedObject = new()
        {
            Type = JsonSchemaType.Object,
            AllOf = [
                new OpenApiSchemaReference("subNS.derivedObject", document),
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema> {
                        {
                            "moreSpecial", new OpenApiSchema {
                                Type = JsonSchemaType.String
                            }
                        }
                    }
                }
            ],
        };

        document.AddComponent("subNS.baseObject", baseObject);
        document.AddComponent("subNS.derivedObject", derivedObject);
        document.AddComponent("subNS.secondLevelDerivedObject", secondLevelDerivedObject);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.derivedType");
        Assert.NotNull(requestBuilderNS);
        CodeClass? requestBuilderClass = requestBuilderNS.FindChildByName<CodeClass>("derivedTypeRequestBuilder", false);
        Assert.NotNull(requestBuilderClass);
        CodeMethod? requestExecutorMethod = requestBuilderClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(requestExecutorMethod);
        Assert.IsType<CodeType>(requestExecutorMethod.ReturnType);
        Assert.Contains("derivedObject", requestExecutorMethod.ReturnType.Name);
        CodeClass? derivedObjectClass = codeModel.FindChildByName<CodeClass>("derivedObject");
        Assert.NotNull(derivedObjectClass);
        CodeMethod? factoryMethod = derivedObjectClass.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Factory));
        Assert.NotNull(factoryMethod);
        Assert.Equal("kind", derivedObjectClass.DiscriminatorInformation.DiscriminatorPropertyName);
        Assert.NotEmpty(derivedObjectClass.DiscriminatorInformation.DiscriminatorMappings);
    }
    [InlineData(JsonSchemaType.String, "", "string")]// https://spec.openapis.org/registry/format/
    [InlineData(JsonSchemaType.String, "commonmark", "string")]
    [InlineData(JsonSchemaType.String, "html", "string")]
    [InlineData(JsonSchemaType.String, "date-time", "DateTimeOffset")]
    [InlineData(JsonSchemaType.String, "duration", "TimeSpan")]
    [InlineData(JsonSchemaType.String, "date", "DateOnly")]
    [InlineData(JsonSchemaType.String, "time", "TimeOnly")]
    [InlineData(JsonSchemaType.String, "base64url", "base64url")]
    [InlineData(JsonSchemaType.String, "uuid", "Guid")]
    // floating points can only be declared as numbers
    [InlineData(JsonSchemaType.Number, "double", "double")]
    [InlineData(JsonSchemaType.Number, "float", "float")]
    [InlineData(JsonSchemaType.Number, "decimal", "decimal")]
    // integers can only be declared as numbers or integers
    [InlineData(JsonSchemaType.Number, "int32", "integer")]
    [InlineData(JsonSchemaType.Integer, "int32", "integer")]
    [InlineData(JsonSchemaType.Number, "int64", "int64")]
    [InlineData(JsonSchemaType.Integer, "int64", "int64")]
    [InlineData(JsonSchemaType.Number, "int8", "sbyte")]
    [InlineData(JsonSchemaType.Integer, "int8", "sbyte")]
    [InlineData(JsonSchemaType.Number, "int16", "integer")]
    [InlineData(JsonSchemaType.Integer, "int16", "integer")]
    [InlineData(JsonSchemaType.Number, "uint8", "byte")]
    [InlineData(JsonSchemaType.Integer, "uint8", "byte")]
    [InlineData(JsonSchemaType.Number, "", "double")]
    [InlineData(JsonSchemaType.Integer, "", "integer")]
    [InlineData(JsonSchemaType.Boolean, "", "boolean")]
    [InlineData(JsonSchemaType.String, "byte", "base64")]
    [InlineData(JsonSchemaType.String, "binary", "binary")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "", "string")]// https://spec.openapis.org/registry/format/
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "commonmark", "string")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "html", "string")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "date-time", "DateTimeOffset")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "duration", "TimeSpan")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "date", "DateOnly")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "time", "TimeOnly")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "base64url", "base64url")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "uuid", "Guid")]
    // floating points can only be declared as numbers
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "double", "double")]
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "float", "float")]
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "decimal", "decimal")]
    // integers can only be declared as numbers or integers
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "int32", "integer")]
    [InlineData(JsonSchemaType.Integer | JsonSchemaType.Null, "int32", "integer")]
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "int64", "int64")]
    [InlineData(JsonSchemaType.Integer | JsonSchemaType.Null, "int64", "int64")]
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "int8", "sbyte")]
    [InlineData(JsonSchemaType.Integer | JsonSchemaType.Null, "int8", "sbyte")]
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "int16", "integer")]
    [InlineData(JsonSchemaType.Integer | JsonSchemaType.Null, "int16", "integer")]
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "uint8", "byte")]
    [InlineData(JsonSchemaType.Integer | JsonSchemaType.Null, "uint8", "byte")]
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "", "double")]
    [InlineData(JsonSchemaType.Integer | JsonSchemaType.Null, "", "integer")]
    [InlineData(JsonSchemaType.Boolean | JsonSchemaType.Null, "", "boolean")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "byte", "base64")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "binary", "binary")]
    [Theory]
    public void MapsPrimitiveFormats(JsonSchemaType type, string format, string expected)
    {
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["primitive"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = type,
                                                Format = format
                                            }
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? requestBuilder = codeModel.FindChildByName<CodeClass>("primitiveRequestBuilder");
        Assert.NotNull(requestBuilder);
        CodeMethod? method = requestBuilder.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(method);
        Assert.Equal(expected, method.ReturnType.Name);
        Assert.True(method.ReturnType.AllTypes.First().IsExternal);
    }
    [InlineData(JsonSchemaType.String, "", "string")]// https://spec.openapis.org/registry/format/
    [InlineData(JsonSchemaType.String, "commonmark", "string")]
    [InlineData(JsonSchemaType.String, "html", "string")]
    [InlineData(JsonSchemaType.String, "date-time", "DateTimeOffset")]
    [InlineData(JsonSchemaType.String, "duration", "TimeSpan")]
    [InlineData(JsonSchemaType.String, "date", "DateOnly")]
    [InlineData(JsonSchemaType.String, "time", "TimeOnly")]
    [InlineData(JsonSchemaType.String, "base64url", "base64url")]
    // floating points can only be declared as numbers
    [InlineData(JsonSchemaType.Number, "double", "double")]
    [InlineData(JsonSchemaType.Number, "float", "float")]
    [InlineData(JsonSchemaType.Number, "decimal", "decimal")]
    // integers can only be declared as numbers or integers
    [InlineData(JsonSchemaType.Number, "int32", "integer")]
    [InlineData(JsonSchemaType.Integer, "int32", "integer")]
    [InlineData(JsonSchemaType.Number, "int64", "int64")]
    [InlineData(JsonSchemaType.Integer, "int64", "int64")]
    [InlineData(JsonSchemaType.Number, "int8", "sbyte")]
    [InlineData(JsonSchemaType.Integer, "int8", "sbyte")]
    [InlineData(JsonSchemaType.Number, "uint8", "byte")]
    [InlineData(JsonSchemaType.Integer, "uint8", "byte")]
    [InlineData(JsonSchemaType.Number, "", "double")]
    [InlineData(JsonSchemaType.Integer, "", "integer")]
    [InlineData(JsonSchemaType.Boolean, "", "boolean")]
    [InlineData(JsonSchemaType.String, "byte", "base64")]
    [InlineData(JsonSchemaType.String, "binary", "binary")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "", "string")]// https://spec.openapis.org/registry/format/
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "commonmark", "string")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "html", "string")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "date-time", "DateTimeOffset")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "duration", "TimeSpan")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "date", "DateOnly")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "time", "TimeOnly")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "base64url", "base64url")]
    // floating points can only be declared as numbers
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "double", "double")]
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "float", "float")]
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "decimal", "decimal")]
    // integers can only be declared as numbers or integers
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "int32", "integer")]
    [InlineData(JsonSchemaType.Integer | JsonSchemaType.Null, "int32", "integer")]
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "int64", "int64")]
    [InlineData(JsonSchemaType.Integer | JsonSchemaType.Null, "int64", "int64")]
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "int8", "sbyte")]
    [InlineData(JsonSchemaType.Integer | JsonSchemaType.Null, "int8", "sbyte")]
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "uint8", "byte")]
    [InlineData(JsonSchemaType.Integer | JsonSchemaType.Null, "uint8", "byte")]
    [InlineData(JsonSchemaType.Number | JsonSchemaType.Null, "", "double")]
    [InlineData(JsonSchemaType.Integer | JsonSchemaType.Null, "", "integer")]
    [InlineData(JsonSchemaType.Boolean | JsonSchemaType.Null, "", "boolean")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "byte", "base64")]
    [InlineData(JsonSchemaType.String | JsonSchemaType.Null, "binary", "binary")]
    [Theory]
    public void MapsQueryParameterTypes(JsonSchemaType type, string format, string expected)
    {
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["primitive"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Parameters = [
                                new OpenApiParameter() {
                                    Name = "query",
                                    In = ParameterLocation.Query,
                                    Schema = new OpenApiSchema {
                                        Type = type,
                                        Format = format
                                    }
                                }
                            ],
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse()
                            }
                        }
                    }
                }
            },
        };
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? queryParameters = codeModel.FindChildByName<CodeClass>("primitiveRequestBuilderGetQueryParameters");
        Assert.NotNull(queryParameters);
        CodeProperty property = queryParameters.Properties.First(static x => x.Name.Equals("query", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.Equal(expected, property.Type.Name);
        Assert.True(property.Type.AllTypes.First().IsExternal);
    }
    [Fact]
    public void IncludesQueryParameterInUriTemplate()
    {
        string documentJSON =
    """
{
	"openapi": "3.1.0",
	"info": {
		"title": "Rest API",
		"version": "25.05.15",
		"description": ""
	},
	"paths": {
		"/api/contracts/": {
			"get": {
				"operationId": "rest_backend_api_contract_get_contracts",
				"summary": "Get Contracts",
				"parameters": [
					{
						"in": "query",
						"name": "type",
						"schema": {
							"allOf": [
								{
									"enum": [
										"PROPERTY",
										"LEASE",
										"SUBLEASE",
										"EXCHANGE"
									],
									"title": "ContractType",
									"type": "string"
								}
							]
						},
						"required": false
					}
				],
				"responses": {
					"200": {
						"description": "OK",
						"content": {
							"application/json": {
								"schema": {
									"type": "string"
								}
							}
						}
					}
				},
				"tags": [
					"Vertrag"
				]
			}
		},
		"/api/contracts/lease/": {
			"get": {
				"operationId": "rest_backend_api_contract_get_lease_contracts",
				"summary": "Get Lease Contracts",
				"parameters": [],
				"responses": {
					"200": {
						"description": "OK",
						"content": {
							"application/json": {
								"schema": {
									"items": {
										"type": "string"
									},
									"title": "Response",
									"type": "array"
								}
							}
						}
					}
				}
			}
		}
	}
}
""";
        (OpenApiDocument? document, Microsoft.OpenApi.Reader.OpenApiDiagnostic? _) = OpenApiDocument.Parse(documentJSON, OpenApiConstants.Json);
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? contractsRequestBuilder = codeModel.FindChildByName<CodeClass>("ContractsRequestBuilder");
        Assert.NotNull(contractsRequestBuilder);
        CodeProperty contractsProperty = contractsRequestBuilder.Properties.First(static x => x.Kind is CodePropertyKind.UrlTemplate);
        Assert.NotNull(contractsProperty);
        Assert.Equal("\"{+baseurl}/api/contracts\"", contractsProperty.DefaultValue);
        CodeClass? emptyRequestBuilder = codeModel.FindChildByName<CodeClass>("EmptyPathSegmentRequestBuilder");
        Assert.NotNull(emptyRequestBuilder);
        CodeProperty emptyProperty = emptyRequestBuilder.Properties.First(static x => x.Kind is CodePropertyKind.UrlTemplate);
        Assert.NotNull(emptyProperty);
        Assert.Equal("\"{+baseurl}/api/contracts/{?type*}\"", emptyProperty.DefaultValue);
    }
    [Fact]
    public void MapsArrayOfTypesAsUnionType()
    {
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["primitive"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = JsonSchemaType.Number | JsonSchemaType.String,
                                            }
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? requestBuilder = codeModel.FindChildByName<CodeClass>("primitiveRequestBuilder");
        Assert.NotNull(requestBuilder);
        CodeMethod? method = requestBuilder.GetChildElements(true).OfType<CodeMethod>().FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(method);
        CodeUnionType unionType = Assert.IsType<CodeUnionType>(method.ReturnType);
        Assert.Equal(2, unionType.Types.Count());
        Assert.Contains("string", unionType.Types.Select(static x => x.Name), StringComparer.OrdinalIgnoreCase);
        Assert.Contains("double", unionType.Types.Select(static x => x.Name), StringComparer.OrdinalIgnoreCase);
    }
    [Fact]
    public void MapsQueryParameterArrayTypes()
    {
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["primitive"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Parameters = [
                                new OpenApiParameter() {
                                    Name = "query",
                                    In = ParameterLocation.Query,
                                    Schema = new OpenApiSchema {
                                        Type = JsonSchemaType.Array,
                                        Items = new OpenApiSchema {
                                            Type = JsonSchemaType.Integer,
                                            Format = "int64"
                                        }
                                    }
                                }
                            ],
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse()
                            }
                        }
                    }
                }
            },
        };
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? queryParameters = codeModel.FindChildByName<CodeClass>("primitiveRequestBuilderGetQueryParameters");
        Assert.NotNull(queryParameters);
        CodeProperty property = queryParameters.Properties.First(static x => x.Name.Equals("query", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.Equal("int64", property.Type.Name);
        Assert.Equal(CodeTypeBase.CodeTypeCollectionKind.Array, property.Type.CollectionKind);
        Assert.True(property.Type.AllTypes.First().IsExternal);
    }

    [Fact]
    public void MapsEnumQueryParameterType()
    {
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["primitive"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Parameters = [
                                new OpenApiParameter() {
                                    Name = "query",
                                    In = ParameterLocation.Query,
                                    Schema = new OpenApiSchema {
                                        Type = JsonSchemaType.String,
                                        Enum = [
                                            "value1",
                                            "value2"
                                        ]
                                    }
                                }
                            ],
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse()
                            }
                        }
                    }
                }
            },
        };
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost", Language = GenerationLanguage.CSharp }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? queryParameters = codeModel.FindChildByName<CodeClass>("primitiveRequestBuilderGetQueryParameters");
        Assert.NotNull(queryParameters);
    }

    [Fact]
    public async Task AddsQueryParameterTypesAsModelsAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, @$"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://api.funtranslations.com
paths:
  /enumeration:
    get:
      parameters:
        - name: InternalExternal
          in: query
          required: true
          schema:
            $ref: '#/components/schemas/InternalExternal'
      responses:
        '200':
          content:
            application/json:
              schema:
                type: string
components:
  schemas:
    InternalExternal:
      enum: [All, Internal, External]
      type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = "https://api.apis.guru/v2/specs/funtranslations.com/starwars/2.3/swagger.json", Language = GenerationLanguage.CSharp }, _httpClient);
        await using FileStream fs = new(tempFilePath, FileMode.Open);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        CodeNamespace codeModel = builder.CreateSourceModel(node);

        CodeClass? queryParameters = codeModel.FindChildByName<CodeClass>("enumerationRequestBuilderGetQueryParameters");
        Assert.NotNull(queryParameters);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("ApiSdk.Models");
        Assert.NotNull(modelsNS);
        CodeEnum? enumType = modelsNS.FindChildByName<CodeEnum>("InternalExternal", false);
        Assert.NotNull(enumType);
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public void MapsQueryParameterCollectionKinds(bool isArray)
    {
        OpenApiSchema baseSchema = new()
        {
            Type = JsonSchemaType.Number,
            Format = "int64"
        };
        OpenApiSchema arraySchema = new()
        {
            Type = JsonSchemaType.Array,
            Items = baseSchema
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["primitive"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Parameters = [
                                new OpenApiParameter {
                                    Name = "query",
                                    In = ParameterLocation.Query,
                                    Schema = isArray ? arraySchema : baseSchema
                                }
                            ],
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse()
                            }
                        }
                    }
                }
            },
        };
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? queryParameters = codeModel.FindChildByName<CodeClass>("primitiveRequestBuilderGetQueryParameters");
        Assert.NotNull(queryParameters);
        CodeProperty property = queryParameters.Properties.First(static x => x.Name.Equals("query", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.Equal("int64", property.Type.Name);
        Assert.Equal(isArray ? CodeTypeBase.CodeTypeCollectionKind.Array : CodeTypeBase.CodeTypeCollectionKind.None, property.Type.CollectionKind);
        Assert.True(property.Type.AllTypes.First().IsExternal);
    }
    [Fact]
    public void DefaultsQueryParametersWithNoSchemaToString()
    {
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["primitive"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Parameters = [
                                new OpenApiParameter {
                                    Name = "query",
                                    In = ParameterLocation.Query
                                }
                            ],
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse()
                            }
                        }
                    }
                }
            },
        };
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? queryParameters = codeModel.FindChildByName<CodeClass>("primitiveRequestBuilderGetQueryParameters");
        Assert.NotNull(queryParameters);
        CodeProperty property = queryParameters.Properties.First(static x => x.Name.Equals("query", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.Equal("string", property.Type.Name);
        Assert.True(property.Type.AllTypes.First().IsExternal);
    }
    [Fact]
    public void DoesntGenerateNamespacesWhenNotRequired()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        CodeClass? myObjectModel = modelsNS.FindChildByName<CodeClass>("Myobject", false);
        Assert.NotNull(myObjectModel);
        CodeNamespace? modelsSubNS = codeModel.FindNamespaceByName("TestSdk.Models.Myobject");
        Assert.Null(modelsSubNS);
    }
    [Fact]
    public void GeneratesNamesapacesWhenRequired()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("subns.myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("subns.myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        Assert.Null(codeModel.FindChildByName<CodeClass>("Myobject", false));
        CodeNamespace? modelsSubNS = codeModel.FindNamespaceByName("TestSdk.Models.subns");
        Assert.NotNull(modelsSubNS);
        Assert.NotNull(modelsSubNS.FindChildByName<CodeClass>("Myobject", false));
    }
    [Fact]
    public void IdsResultInIndexers()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answers/{id}"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.Null(codeModel.FindChildByName<CodeClass>("With"));
        Assert.Null(codeModel.FindChildByName<CodeClass>("WithResponse"));
        CodeNamespace? rbNS = codeModel.FindNamespaceByName("TestSdk.Answers.Item");
        Assert.NotNull(rbNS);
        CodeClass rb = rbNS.Classes.First();
        Assert.Equal("AnswersItemRequestBuilder", rb.Name);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        Assert.Null(modelsNS.FindChildByName<CodeClass>("With", false));
    }
    [Fact]
    public void HandlesCollectionOfEnumSchemasInAnyOfWithNullable()
    {
        OpenApiSchema enumSchema = new()
        {
            Title = "riskLevel",
            Enum =
            [
                "low",
                "medium",
                "high",
                "hidden",
                "none",
                "unknownFutureValue"
            ],
            Type = JsonSchemaType.String
        };
        OpenApiSchema myObjectSchema = new()
        {
            Title = "conditionalAccessConditionSet",
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "signInRiskLevels", new OpenApiSchema {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema
                        {
                            AnyOf =
                            [
                                new OpenApiSchemaReference("riskLevel"),
                                new OpenApiSchema
                                {
                                    Type = JsonSchemaType.Object,
                                }
                            ]
                        }
                    }
                }
            },
        };

        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            }
        };
        document.AddComponent("myobject", myObjectSchema);
        document.AddComponent("riskLevel", enumSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        CodeClass? responseClass = modelsNS.Classes.FirstOrDefault(static x => x.IsOfKind(CodeClassKind.Model) && x.Name.Equals("myobject", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(responseClass);
        CodeProperty? property = responseClass.Properties.FirstOrDefault(static x => x.IsOfKind(CodePropertyKind.Custom) && x.Name.Equals("signInRiskLevels", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.NotEmpty(property.Type.Name);
        CodeType? codeType = property.Type as CodeType;
        Assert.NotNull(codeType);
        Assert.IsType<CodeEnum>(codeType.TypeDefinition);// Ensure the collection is a codeEnum
        Assert.Equal(CodeTypeBase.CodeTypeCollectionKind.Complex, codeType.CollectionKind);// Ensure the collection is a codeEnum
    }
    [Fact]
    public void HandlesCollectionOfEnumSchemas()
    {
        OpenApiSchema enumSchema = new()
        {
            Title = "riskLevel",
            Enum =
            [
                "low",
                "medium",
                "high",
                "hidden",
                "none",
                "unknownFutureValue"
            ],
            Type = JsonSchemaType.String
        };
        OpenApiSchema myObjectSchema = new()
        {
            Title = "conditionalAccessConditionSet",
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "signInRiskLevels", new OpenApiSchema {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchemaReference("riskLevel")
                    }
                }
            },
        };

        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("myobject", myObjectSchema);
        document.AddComponent("riskLevel", enumSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        CodeClass? responseClass = modelsNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.Model) && x.Name.Equals("myobject", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(responseClass);
        CodeProperty? property = responseClass.Properties.FirstOrDefault(x => x.IsOfKind(CodePropertyKind.Custom) && x.Name.Equals("signInRiskLevels", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.NotEmpty(property.Type.Name);
        CodeType? codeType = property.Type as CodeType;
        Assert.NotNull(codeType);
        Assert.IsType<CodeEnum>(codeType.TypeDefinition);// Ensure the collection is a codeEnum
        Assert.Equal(CodeTypeBase.CodeTypeCollectionKind.Complex, codeType.CollectionKind);// Ensure the collection is a codeEnum
    }
    [Fact]
    public void InlinePropertiesGenerateTypes()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "tilleggsinformasjon", new OpenApiSchema {
                        Type = JsonSchemaType.Object,
                        AdditionalProperties = new OpenApiSchema {
                            Type = JsonSchemaType.String
                        }
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            }
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        CodeClass? responseClass = modelsNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.Model) && x.Name.Equals("myobject", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(responseClass);
        CodeProperty? property = responseClass.Properties.FirstOrDefault(x => x.IsOfKind(CodePropertyKind.Custom) && x.Name.Equals("Tilleggsinformasjon", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(property);
        Assert.NotEmpty(property.Type.Name);
    }
    [Fact]
    public void ModelsDoesntUsePathDescriptionWhenAvailable()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Description = "some path item description",
                    Summary = "some path item summary",
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Description = "some operation description",
                            Summary = "some operation summary",
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            }
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("TestSdk.Models");
        Assert.NotNull(modelsNS);
        CodeClass? responseClass = modelsNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.Model));
        Assert.NotNull(responseClass);
        Assert.Empty(responseClass.Documentation.DescriptionTemplate);
    }

    [InlineData("application/json")]
    [InlineData("application/json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8")]
    [InlineData("application/vnd.github.mercy-preview+json")]
    [InlineData("application/vnd.github.mercy-preview+json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8")]
    [Theory]
    public void AcceptVendorsTypes(string contentType)
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        [contentType] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        CodeClass? rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        CodeMethod? executorMethod = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Get);
        Assert.NotNull(executorMethod);
        Assert.Equal("myobject", executorMethod.ReturnType.Name);
    }

    [Fact]
    public void ModelsUseDescriptionWhenAvailable()
    {
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Description = "some path item description",
                    Summary = "some path item summary",
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new()
                        {
                            Description = "some operation description",
                            Summary = "some operation summary",
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Description = "some description",
                                                Properties = new Dictionary<string, IOpenApiSchema> {
                                                    {
                                                        "name", new OpenApiSchema {
                                                            Type = JsonSchemaType.String
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            }
        };
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost", }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? modelsSubNS = codeModel.FindNamespaceByName("TestSdk.answer");
        Assert.NotNull(modelsSubNS);
        CodeClass? responseClass = modelsSubNS.FindChildByName<CodeClass>("AnswerGetResponse", false);
        Assert.NotNull(responseClass);
        Assert.Equal("some description", responseClass.Documentation.DescriptionTemplate);

        CodeClass? obsoleteResponseClass = modelsSubNS.FindChildByName<CodeClass>("AnswerResponse", false);

        Assert.Null(obsoleteResponseClass);

        CodeClass? requestBuilderClass = modelsSubNS.Classes.FirstOrDefault(static c => c.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(requestBuilderClass);
        Assert.Equal("some path item description", requestBuilderClass.Documentation.DescriptionTemplate);

        Assert.Single(requestBuilderClass.Methods, static x => x.Kind is CodeMethodKind.RequestExecutor);

        CodeProperty? responseProperty = codeModel.FindNamespaceByName("TestSdk")?.Classes.SelectMany(c => c.Properties).FirstOrDefault(static p => p.Kind == CodePropertyKind.RequestBuilder);
        Assert.NotNull(responseProperty);
        Assert.Equal("some path item description", responseProperty.Documentation.DescriptionTemplate);
    }

    [Fact]
    public void Considers200WithSchemaOver2XXWithSchema()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiSchema myOtherObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["2XX"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myotherobject")
                                        }
                                    }
                                },
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("myobject", myObjectSchema);
        document.AddComponent("myotherobject", myOtherObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(
            mockLogger.Object,
            new GenerationConfiguration
            {
                ClientClassName = "TestClient",
                ClientNamespaceName = "TestSdk",
                ApiRootUrl = "https://localhost",
                StructuredMimeTypes = new GenerationConfiguration().StructuredMimeTypes
            }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        CodeClass? rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        CodeMethod? executor = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executor);
        Assert.Equal("myobject", executor.ReturnType.Name);
    }
    [Fact]
    public void Considers2XXWithSchemaOver204WithNoSchema()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["2XX"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                                ["204"] = new OpenApiResponse(),
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(
            mockLogger.Object,
            new GenerationConfiguration
            {
                ClientClassName = "TestClient",
                ClientNamespaceName = "TestSdk",
                ApiRootUrl = "https://localhost",
                StructuredMimeTypes = new GenerationConfiguration().StructuredMimeTypes
            }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        CodeClass? rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        CodeMethod? executor = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executor);
        Assert.Equal("myobject", executor.ReturnType.Name);
    }
    [Fact]
    public void Considers204WithNoSchemaOver206WithNoSchema()
    {
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["206"] = new OpenApiResponse(),
                                ["204"] = new OpenApiResponse(),
                            }
                        }
                    }
                }
            },
        };
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(
            mockLogger.Object,
            new GenerationConfiguration
            {
                ClientClassName = "TestClient",
                ClientNamespaceName = "TestSdk",
                ApiRootUrl = "https://localhost",
                StructuredMimeTypes = new GenerationConfiguration().StructuredMimeTypes
            }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        CodeClass? rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        CodeMethod? executor = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executor);
        Assert.Equal("void", executor.ReturnType.Name);
    }
    [InlineData(204)]
    [InlineData(301)]
    [InlineData(302)]
    [InlineData(303)]
    [InlineData(304)]
    [InlineData(307)]
    [Theory]
    public void DoesntGenerateVoidExecutorOnMixedNoContent(int statusCode)
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                                [statusCode.ToString()] = new OpenApiResponse(),
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        CodeClass? rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        Assert.Single(rbClass.Methods, x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        CodeMethod? executor = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executor);
        Assert.NotEqual("void", executor.ReturnType.Name);
    }
    [InlineData(204)]
    [InlineData(301)]
    [InlineData(302)]
    [InlineData(303)]
    [InlineData(304)]
    [InlineData(307)]
    [Theory]
    public void GeneratesVoidReturnTypeForNoContent(int statusCode)
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                [statusCode.ToString()] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        CodeClass? rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        Assert.Single(rbClass.Methods, x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        CodeMethod? executor = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executor);
        Assert.Equal("void", executor.ReturnType.Name);
    }
    [InlineData(new[] { "microsoft.graph.user", "microsoft.graph.termstore.term" }, "microsoft.graph")]
    [InlineData(new[] { "microsoft.graph.user", "odata.errors.error" }, "")]
    [InlineData(new string[] { }, "")]
    [Theory]
    public void StripsCommonModelsPrefix(string[] componentNames, string stripPrefix)
    {
        OpenApiDocument document = new();
        foreach (string componentName in componentNames)
        {
            OpenApiSchema myObjectSchema = new()
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema> {
                    {
                        "id", new OpenApiSchema {
                            Type = JsonSchemaType.String,
                        }
                    }
                },
            };
            document.Paths.Add($"answer{componentName}", new OpenApiPathItem
            {
                Operations = new()
                {
                    [NetHttpMethod.Get] = new OpenApiOperation
                    {
                        Responses = new OpenApiResponses
                        {
                            ["200"] = new OpenApiResponse
                            {
                                Content = new Dictionary<string, IOpenApiMediaType>()
                                {
                                    ["application/json"] = new OpenApiMediaType()
                                    {
                                        Schema = new OpenApiSchemaReference(componentName)
                                    }
                                }
                            },
                        }
                    }
                }
            });
            document.AddComponent(componentName, myObjectSchema);
        }
        document.SetReferenceHostDocument();

        string result = ReQuestyBuilder.GetDeeperMostCommonNamespaceNameForModels(document);
        Assert.Equal(stripPrefix, result);
    }
    [Fact]
    public void HandlesContentParameters()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer(ids={ids}"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Parameters = [
                                new OpenApiParameter {
                                    Name = "ids",
                                    In = ParameterLocation.Path,
                                    Content = new Dictionary<string, IOpenApiMediaType>
                                    {
                                        { "application/json",
                                        new OpenApiMediaType {
                                            Schema = new OpenApiSchema {
                                                                Type = JsonSchemaType.Array,
                                                                Items = new OpenApiSchema {
                                                                    Type = JsonSchemaType.String,
                                                                }
                                                            }
                                            }
                                        }
                                    }
                                }
                            ],
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? answersNS = codeModel.FindNamespaceByName("TestSdk.answerWithIds");
        Assert.NotNull(answersNS);
        CodeClass? rbClass = answersNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        CodeMethod? ctorMethod = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.Constructor));
        Assert.NotNull(ctorMethod);
        CodeParameter? idsParam = ctorMethod.Parameters.FirstOrDefault(x => x.Name.Equals("ids", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(idsParam);
        Assert.Equal("string", idsParam.Type.Name);
        Assert.Equal(CodeTypeBase.CodeTypeCollectionKind.None, idsParam.Type.CollectionKind);
    }

    [Fact]
    public void HandlesPagingExtension()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["users"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Extensions = new Dictionary<string, IOpenApiExtension> {
                                { OpenApiPagingExtension.Name, new OpenApiPagingExtension { NextLinkName = "@odata.nextLink" } }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? answersNS = codeModel.FindNamespaceByName("TestSdk.users");
        Assert.NotNull(answersNS);
        CodeClass? rbClass = answersNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        CodeMethod? executorMethod = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Get);
        Assert.NotNull(executorMethod);
        Assert.Equal("@odata.nextLink", executorMethod.PagingInformation?.NextLinkName);
    }
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public void SetsReadonlyProperties(bool isReadonly)
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                        ReadOnly = isReadonly,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["users"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            }
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "TestClient", ClientNamespaceName = "TestSdk", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? objectClass = codeModel.FindChildByName<CodeClass>("myobject");
        Assert.NotNull(objectClass);
        CodeProperty nameProperty = objectClass.Properties.First(static x => "name".Equals(x.Name, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(isReadonly, nameProperty.ReadOnly);
    }
    [Theory]
    [InlineData("#GET", 0)]
    [InlineData("/#GET", 1)]
    public void SupportsIncludeFilterOnRootPath(string inputPattern, int expectedPathsCount)
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["/"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
            },
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration
        {
            ClientClassName = "TestClient",
            ClientNamespaceName = "TestSdk",
            ApiRootUrl = "https://localhost",
            IncludePatterns = [
                inputPattern
            ]
        }, _httpClient);
        builder.FilterPathsByPatterns(document);
        Assert.Equal(expectedPathsCount, document.Paths.Count);
    }
    [Fact]
    public void SupportsIncludeFilter()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["users"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
                ["groups"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
            }
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration
        {
            ClientClassName = "TestClient",
            ClientNamespaceName = "TestSdk",
            ApiRootUrl = "https://localhost",
            IncludePatterns = [
                "*users*"
            ]
        }, _httpClient);
        builder.FilterPathsByPatterns(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.Null(codeModel.FindNamespaceByName("TestSdk.groups"));
    }
    [Fact]
    public void SupportsExcludeFilter()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["users"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
                ["groups"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
            }
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration
        {
            ClientClassName = "TestClient",
            ClientNamespaceName = "TestSdk",
            ApiRootUrl = "https://localhost",
            ExcludePatterns = [
                "*groups*"
            ]
        }, _httpClient);
        builder.FilterPathsByPatterns(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.Null(codeModel.FindNamespaceByName("TestSdk.groups"));
    }
    [Fact]
    public void SupportsIncludeFilterWithOperation()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["users/{id}/messages"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        },
                        [NetHttpMethod.Post] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody
                            {
                                Content = new Dictionary<string, IOpenApiMediaType>()
                                {
                                    ["application/json"] = new OpenApiMediaType()
                                    {
                                        Schema = new OpenApiSchemaReference("myobject")
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        },
                        [NetHttpMethod.Put] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody
                            {
                                Content = new Dictionary<string, IOpenApiMediaType>()
                                {
                                    ["application/json"] = new OpenApiMediaType()
                                    {
                                        Schema = new OpenApiSchemaReference("myobject")
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
                ["groups"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
                ["students"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
            }
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration
        {
            ClientClassName = "TestClient",
            ClientNamespaceName = "TestSdk",
            ApiRootUrl = "https://localhost",
            IncludePatterns = [
                "users/*/messages*#get,PATCH", // lowercase is voluntary to test case insensitivity
                "users/**#POST",
                "students"
            ]
        }, _httpClient);
        builder.FilterPathsByPatterns(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.Null(codeModel.FindNamespaceByName("TestSdk.groups"));
        CodeNamespace? messagesNS = codeModel.FindNamespaceByName("TestSdk.users.item.messages");
        Assert.NotNull(messagesNS);
        CodeClass? messagesRS = messagesNS.FindChildByName<CodeClass>("MessagesRequestBuilder");
        Assert.NotNull(messagesRS);
        Assert.Single(messagesRS.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Post);
        Assert.Single(messagesRS.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Get);
        Assert.DoesNotContain(messagesRS.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Put);
        CodeNamespace? studentsNS = codeModel.FindNamespaceByName("TestSdk.students");
        Assert.NotNull(studentsNS);
        CodeClass? studentsRS = studentsNS.FindChildByName<CodeClass>("StudentsRequestBuilder");
        Assert.NotNull(studentsRS);
    }
    [Fact]
    public void SupportsIndexingParametersInSubPaths()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["users({userId})/manager"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Parameters = [
                                new OpenApiParameter {
                                    Name = "userId",
                                    In = ParameterLocation.Path,
                                    Required = true,
                                    Schema = new OpenApiSchema {
                                        Type = JsonSchemaType.String
                                    }
                                }
                            ],
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
            }
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration
        {
            ClientClassName = "TestClient",
            ClientNamespaceName = "TestSdk",
            ApiRootUrl = "https://localhost",
        }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? NS = codeModel.FindNamespaceByName("TestSdk.usersWithUserId");
        Assert.NotNull(NS);
        CodeClass? rb = NS.FindChildByName<CodeClass>("usersWithUserIdRequestBuilder");
        Assert.NotNull(rb);
        CodeMethod? method = rb.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.Constructor));
        Assert.NotNull(method);
        Assert.Equal("userId", method.Parameters.Last(static x => x.IsOfKind(CodeParameterKind.Path)).Name);
    }
    [Fact]
    public async Task DisambiguatesOperationsConflictingWithPath1Async()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.0
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
      properties:
        id:
          type: string
        displayName:
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.me");
        Assert.NotNull(requestBuilderNS);
        CodeClass? getRB = requestBuilderNS.FindChildByName<CodeClass>("meRequestBuilder", false);
        Assert.NotNull(getRB);
        Assert.NotNull(getRB.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && "Get".Equals(x.Name, StringComparison.OrdinalIgnoreCase)));
        Assert.NotNull(getRB.Properties.FirstOrDefault(static x => x.IsOfKind(CodePropertyKind.RequestBuilder) && "GetPath".Equals(x.Name, StringComparison.OrdinalIgnoreCase)));
    }
    [Fact]
    public async Task DisambiguatesOperationsConflictingWithPath2Async()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.0
info:
  title: Microsoft Graph get user API
  version: 1.0.0
servers:
  - url: https://graph.microsoft.com/v1.0/
paths:
  /me/get:
    get:
      responses:
        200:
          description: Success!
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.user'
  /me:
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
      properties:
        id:
          type: string
        displayName:
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.me");
        Assert.NotNull(requestBuilderNS);
        CodeClass? getRB = requestBuilderNS.FindChildByName<CodeClass>("meRequestBuilder", false);
        Assert.NotNull(getRB);
        Assert.NotNull(getRB.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && "Get".Equals(x.Name, StringComparison.OrdinalIgnoreCase)));
        Assert.NotNull(getRB.Properties.FirstOrDefault(static x => x.IsOfKind(CodePropertyKind.RequestBuilder) && "GetPath".Equals(x.Name, StringComparison.OrdinalIgnoreCase)));
    }
    [Fact]
    public async Task IndexerAndRequestBuilderNamesMatchAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.0
info:
  title: Microsoft Graph get user API
  version: 1.0.0
servers:
  - url: https://graph.microsoft.com/v1.0/
paths:
  /me/posts/{post-id}:
    get:
      responses:
        200:
          description: Success!
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.post'
components:
  schemas:
    microsoft.graph.post:
      type: object
      properties:
        id:
          type: string
        displayName:
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document!);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? collectionRequestBuilderNamespace = codeModel.FindNamespaceByName("ApiSdk.me.posts");
        Assert.NotNull(collectionRequestBuilderNamespace);
        CodeClass? collectionRequestBuilder = collectionRequestBuilderNamespace.FindChildByName<CodeClass>("postsRequestBuilder");
        CodeIndexer? collectionIndexer = collectionRequestBuilder?.Indexer;
        Assert.NotNull(collectionIndexer);
        Assert.Equal("string", collectionIndexer.IndexParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("Unique identifier of the item", collectionIndexer.IndexParameter.Documentation.DescriptionTemplate, StringComparer.OrdinalIgnoreCase);
        Assert.False(collectionIndexer.Deprecation?.IsDeprecated);
        CodeNamespace? itemRequestBuilderNamespace = codeModel.FindNamespaceByName("ApiSdk.me.posts.item");
        Assert.NotNull(itemRequestBuilderNamespace);
        CodeClass? itemRequestBuilder = itemRequestBuilderNamespace.FindChildByName<CodeClass>("postItemRequestBuilder");
        Assert.NotNull(itemRequestBuilder);
        Assert.Equal(collectionIndexer.ReturnType.Name, itemRequestBuilder.Name);
    }
    [Fact]
    public async Task IndexerTypeIsAccurateAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.0
info:
  title: Microsoft Graph get user API
  version: 1.0.0
servers:
  - url: https://graph.microsoft.com/v1.0/
paths:
  /me/posts/{post-id}:
    get:
      parameters:
        - name: post-id
          in: path
          required: true
          description: The id of the pet to retrieve
          schema:
            type: integer
            format: int32
      responses:
        200:
          description: Success!
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.post'
  /authors/{author-id}/posts:
    get:
      parameters:
        - name: author-id
          in: path
          required: true
          description: The id of the author's posts to retrieve
          schema:
            type: string
            format: uuid
      responses:
        200:
          description: Success!
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.post'
  /actors/{actor-id}/foo/baz:
    get:
      parameters:
        - name: actor-id
          in: path
          required: true
          description: The id of the actor
          schema:
            type: string
            format: uuid
      responses:
        200:
          description: Success!
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.post'
components:
  schemas:
    microsoft.graph.post:
      type: object
      properties:
        id:
          type: string
        displayName:
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document!);
        CodeNamespace codeModel = builder.CreateSourceModel(node);

        CodeNamespace? postsCollectionRequestBuilderNamespace = codeModel.FindNamespaceByName("ApiSdk.me.posts");
        Assert.NotNull(postsCollectionRequestBuilderNamespace);
        CodeClass? postsCollectionRequestBuilder = postsCollectionRequestBuilderNamespace.FindChildByName<CodeClass>("postsRequestBuilder");
        CodeIndexer? postsCollectionIndexer = postsCollectionRequestBuilder?.Indexer;
        Assert.NotNull(postsCollectionIndexer);
        Assert.Equal("integer", postsCollectionIndexer.IndexParameter.Type.Name);
        Assert.Equal("The id of the pet to retrieve", postsCollectionIndexer.IndexParameter.Documentation.DescriptionTemplate, StringComparer.OrdinalIgnoreCase);
        Assert.False(postsCollectionIndexer.IndexParameter.Type.IsNullable);
        Assert.False(postsCollectionIndexer.Deprecation?.IsDeprecated);

        CodeNamespace? postsItemRequestBuilderNamespace = codeModel.FindNamespaceByName("ApiSdk.me.posts.item");
        Assert.NotNull(postsItemRequestBuilderNamespace);
        CodeClass? postsItemRequestBuilder = postsItemRequestBuilderNamespace.FindChildByName<CodeClass>("postItemRequestBuilder");
        Assert.NotNull(postsItemRequestBuilder);
        Assert.Equal(postsCollectionIndexer.ReturnType.Name, postsItemRequestBuilder.Name);

        CodeNamespace? authorsCollectionRequestBuilderNamespace = codeModel.FindNamespaceByName("ApiSdk.authors");
        Assert.NotNull(authorsCollectionRequestBuilderNamespace);
        CodeClass? authorsCollectionRequestBuilder = authorsCollectionRequestBuilderNamespace.FindChildByName<CodeClass>("authorsRequestBuilder");
        CodeIndexer? authorsCollectionIndexer = authorsCollectionRequestBuilder?.Indexer;
        Assert.NotNull(authorsCollectionIndexer);
        Assert.Equal("Guid", authorsCollectionIndexer.IndexParameter.Type.Name);
        Assert.Equal("The id of the author's posts to retrieve", authorsCollectionIndexer.IndexParameter.Documentation.DescriptionTemplate, StringComparer.OrdinalIgnoreCase);
        Assert.False(authorsCollectionIndexer.IndexParameter.Type.IsNullable);
        Assert.False(authorsCollectionIndexer.Deprecation?.IsDeprecated);

        CodeNamespace? authorsItemRequestBuilderNamespace = codeModel.FindNamespaceByName("ApiSdk.authors.item");
        Assert.NotNull(authorsItemRequestBuilderNamespace);
        CodeClass? authorsItemRequestBuilder = authorsItemRequestBuilderNamespace.FindChildByName<CodeClass>("authorItemRequestBuilder");
        Assert.NotNull(authorsItemRequestBuilder);
        Assert.Equal(authorsCollectionIndexer.ReturnType.Name, authorsItemRequestBuilder.Name);

        CodeNamespace? actorsCollectionRequestBuilderNamespace = codeModel.FindNamespaceByName("ApiSdk.actors");
        Assert.NotNull(actorsCollectionRequestBuilderNamespace);

        CodeClass? actorsCollectionRequestBuilder = actorsCollectionRequestBuilderNamespace.FindChildByName<CodeClass>("actorsRequestBuilder");
        CodeIndexer? actorsCollectionIndexer = actorsCollectionRequestBuilder?.Indexer;
        Assert.NotNull(actorsCollectionIndexer);
        Assert.Equal("Guid", actorsCollectionIndexer.IndexParameter.Type.Name);
        Assert.Equal("The id of the actor", actorsCollectionIndexer.IndexParameter.Documentation.DescriptionTemplate, StringComparer.OrdinalIgnoreCase);
        Assert.False(actorsCollectionIndexer.IndexParameter.Type.IsNullable);
        Assert.False(actorsCollectionIndexer.Deprecation?.IsDeprecated);

        CodeNamespace? actorsItemRequestBuilderNamespace = codeModel.FindNamespaceByName("ApiSdk.actors.item");
        Assert.NotNull(actorsItemRequestBuilderNamespace);
        CodeClass? actorsItemRequestBuilder = actorsItemRequestBuilderNamespace.FindChildByName<CodeClass>("actorItemRequestBuilder");
        Assert.NotNull(actorsItemRequestBuilder);
        Assert.Equal(actorsCollectionIndexer.ReturnType.Name, actorsItemRequestBuilder.Name);
    }

    [Fact]
    public async Task MapsBooleanEnumToBooleanTypeAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.0
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
                                type: boolean
                                enum:
                                    - true
                                    - false");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document!);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.me");
        Assert.NotNull(requestBuilderNS);
        CodeClass? getRB = requestBuilderNS.FindChildByName<CodeClass>("meRequestBuilder", false);
        Assert.NotNull(getRB);
        CodeMethod? getMethod = getRB.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && "Get".Equals(x.Name, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(getMethod);
        Assert.Equal("boolean", getMethod.ReturnType.Name);
    }
    [Fact]
    public async Task MapsNumberEnumToDoubleTypeAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.0
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
                                type: number
                                enum:
                                    - 1
                                    - 2");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document!);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.me");
        Assert.NotNull(requestBuilderNS);
        CodeClass? getRB = requestBuilderNS.FindChildByName<CodeClass>("meRequestBuilder", false);
        Assert.NotNull(getRB);
        CodeMethod? getMethod = getRB.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && "Get".Equals(x.Name, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(getMethod);
        Assert.Equal("double", getMethod.ReturnType.Name);
    }
    [InlineData("MV22X/MV72X", "MV22XMV72X")]
    [Theory]
    public async Task CleansInlineTypeNamesAsync(string raw, string expected)
    {
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, @$"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://localhost:443
paths:
  /enumeration:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: object
                properties:
                  {raw}:
                    type: object
                    properties:
                      foo:
                        type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = "https://localhost:443" }, _httpClient);
        await using FileStream fs = new(tempFilePath, FileMode.Open);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        CodeClass? inlineType = rootNS.FindChildByName<CodeClass>($"enumerationGetResponse_{expected}", true);
        Assert.NotNull(inlineType);
    }
    [Fact]
    public void AddReservedPathParameterSymbol()
    {
        OpenApiSchema userSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                },
                {
                    "displayName", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["users/{id}/manager"] = new OpenApiPathItem
                {
                    Parameters = [
                        new OpenApiParameter {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = JsonSchemaType.String
                            },
                            Extensions = new Dictionary<string, IOpenApiExtension> (){
                                ["x-ms-reserved-parameter"] = new OpenApiReservedParameterExtension {
                                    IsReserved = true
                                }
                            }
                        }
                    ],
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("microsoft.graph.user")
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
        };
        document.AddComponent("microsoft.graph.user", userSchema);
        document.SetReferenceHostDocument();
        CountLogger<ReQuestyBuilder> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? managerRB = codeModel.FindNamespaceByName("ApiSdk.users.item.manager")?.FindChildByName<CodeClass>("ManagerRequestBuilder", false);
        Assert.NotNull(managerRB);
        CodeProperty? managerUrlTemplate = managerRB.FindChildByName<CodeProperty>("UrlTemplate", false);
        Assert.NotNull(managerUrlTemplate);
        Assert.Equal("{+baseurl}/users/{+id}/manager", managerUrlTemplate.DefaultValue.Trim('"'));
    }
    [Fact]
    public void DoesNotAddReservedPathParameterSymbol()
    {
        OpenApiSchema userSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "id", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                },
                {
                    "displayName", new OpenApiSchema {
                        Type = JsonSchemaType.String
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["users/{id}/manager"] = new OpenApiPathItem
                {
                    Parameters = [
                        new OpenApiParameter {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema {
                                Type = JsonSchemaType.String
                            },
                            Extensions = new Dictionary<string, IOpenApiExtension>() {
                                ["x-ms-reserved-parameter"] = new OpenApiReservedParameterExtension {
                                    IsReserved = false
                                }
                            }
                        }
                    ],
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("microsoft.graph.user")
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            },
        };
        document.AddComponent("microsoft.graph.user", userSchema);
        document.SetReferenceHostDocument();
        CountLogger<ReQuestyBuilder> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger, new GenerationConfiguration { ClientClassName = "Graph", ApiRootUrl = "https://localhost" }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? managerRB = codeModel.FindNamespaceByName("ApiSdk.users.item.manager")?.FindChildByName<CodeClass>("ManagerRequestBuilder", false);
        Assert.NotNull(managerRB);
        CodeProperty? managerUrlTemplate = managerRB.FindChildByName<CodeProperty>("UrlTemplate", false);
        Assert.NotNull(managerUrlTemplate);
        Assert.Equal("{+baseurl}/users/{id}/manager", managerUrlTemplate.DefaultValue.Trim('"'));
    }
    [Fact]
    public async Task MergesIntersectionTypesAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                allOf:
                  - $ref: '#/components/schemas/microsoft.graph.entity'
                  - $ref: '#/components/schemas/microsoft.graph.directoryObject'
                  - $ref: '#/components/schemas/microsoft.graph.user'
components:
  schemas:
    microsoft.graph.entity:
      title: entity
      type: object
      properties:
        id:
          type: string
        '@odata.type':
          type: string
    microsoft.graph.directoryObject:
      title: directoryObject
      type: object
      properties:
        deletedDateTime:
          pattern: '^[0-9]{4,}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]([.][0-9]{1,12})?(Z|[+-][0-9][0-9]:[0-9][0-9])$'
          type: string
          format: date-time
          nullable: true
    microsoft.graph.user:
      title: user
      type: object
      properties:
        accountEnabled:
          type: boolean
          nullable: true");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? resultClass = codeModel.FindChildByName<CodeClass>("DirectoryObjectGetResponse");
        Assert.NotNull(resultClass);
        Assert.Equal(4, resultClass.Properties.Count(static x => x.IsOfKind(CodePropertyKind.Custom)));
    }
    [Fact]
    public async Task SkipsInvalidItemsPropertiesAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                type: object
                properties:
                  datasets:
                    type: array
                    items: true
                  datakeys:
                    type: array
                    items: {}
                  datainfo:
                    type: array
                  id:
                    type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? resultClass = codeModel.FindChildByName<CodeClass>("DirectoryObjectGetResponse");
        Assert.NotNull(resultClass);
        HashSet<string> keysToCheck = new(StringComparer.OrdinalIgnoreCase) { "datasets", "datakeys", "datainfo" };
        CodeProperty[] propertiesToValidate = resultClass.Properties.Where(x => x.IsOfKind(CodePropertyKind.Custom) && keysToCheck.Contains(x.Name)).ToArray();
        Assert.NotNull(propertiesToValidate);
        Assert.NotEmpty(propertiesToValidate);
        Assert.Equal(keysToCheck.Count, propertiesToValidate.Length);// all the properties are present
        Assert.Single(resultClass.Properties, static x => x.IsOfKind(CodePropertyKind.Custom) && x.Name.Equals("id", StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task GetsCorrectInheritedInlineSchemaNameAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.3
servers:
- url: https://api.github.com
info:
  title: GitHub API
  version: 1.0.0
paths:
  '/app-manifests/{code}/conversions':
    post:
      operationId: apps/create-from-manifest
      parameters:
      - in: path
        name: code
        required: true
        schema:
          type: string
      responses:
        '201':
          content:
            application/json:
              schema:
                allOf:
                - '$ref': '#/components/schemas/integration'
                - additionalProperties: true
                  properties:
                    client_id:
                      type: string
                    client_secret:
                      type: string
                    pem:
                      type: string
                    webhook_secret:
                      nullable: true
                      type: string
                  type: object
          description: Response
components:
  schemas:
    integration:
      properties:
        client_id:
          type: string
      title: GitHub app
      type: object");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.NotNull(codeModel.FindChildByName<CodeClass>("ConversionsPostResponse"));
    }
    [Fact]
    public async Task DescriptionTakenFromAllOfAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.directoryObject'
components:
  schemas:
    microsoft.graph.entity:
      title: entity
      description: 'base entity'
      type: object
      properties:
        id:
          type: string
        '@odata.type':
          type: string
      discriminator:
        propertyName: '@odata.type'
        mapping:
          '#microsoft.graph.directoryObject': '#/components/schemas/microsoft.graph.directoryObject'
          '#microsoft.graph.sub1': '#/components/schemas/microsoft.graph.sub1'
          '#microsoft.graph.sub2': '#/components/schemas/microsoft.graph.sub2'
    microsoft.graph.directoryObject:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.entity'
        - title: directoryObject
          description: 'directory object'
          type: object
          required: [ '@odata.type' ]
          discriminator:
            propertyName: '@odata.type'
            mapping:
              '#microsoft.graph.sub1': '#/components/schemas/microsoft.graph.sub1'
              '#microsoft.graph.sub2': '#/components/schemas/microsoft.graph.sub2'
    microsoft.graph.sub1:
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.directoryObject'
        - title: sub1
          description: 'sub1'
          type: object
    microsoft.graph.sub2:
      description: 'sub2'
      allOf:
        - $ref: '#/components/schemas/microsoft.graph.directoryObject'
        - title: sub2
          description: 'ignored'
          type: object");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.Equal("base entity", codeModel.FindChildByName<CodeClass>("entity")?.Documentation.DescriptionTemplate);
        Assert.Equal("directory object", codeModel.FindChildByName<CodeClass>("directoryObject")?.Documentation.DescriptionTemplate);
        Assert.Equal("sub1", codeModel.FindChildByName<CodeClass>("sub1")?.Documentation.DescriptionTemplate);
        Assert.Equal("sub2", codeModel.FindChildByName<CodeClass>("sub2")?.Documentation.DescriptionTemplate);
    }
    [Fact]
    public async Task CleanupSymbolNameDoesNotCauseNameConflictsAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: Example
  description: Example
  version: 1.0.1
servers:
  - url: https://example.org
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/entity'
components:
  schemas:
    entity:
      title: entity
      type: object
      required: ['type', '@type']
      properties:
        type:
          type: string
        '@type':
          type: integer");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? resultClass = codeModel.FindChildByName<CodeClass>("Entity");
        Assert.NotNull(resultClass);
        Assert.Equal(2, resultClass.Properties.Select(static x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
    [Fact]
    public async Task CleanupSymbolNameDoesNotCauseNameConflictsWithSuperTypeAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: Example
  description: Example
  version: 1.0.1
servers:
  - url: https://example.org
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/subtype'
components:
  schemas:
    entity:
      title: entity
      type: object
      required: ['@type']
      properties:
        '@type':
          type: integer
      discriminator:
        propertyName: '@type'
        mapping:
          'subtype': '#/components/schemas/subtype'
    subtype:
      allOf:
        - $ref: '#/components/schemas/entity'
        - title: subtype
          type: object
          required: ['type', '@type']
          properties:
            'type':
              type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? entityClass = codeModel.FindChildByName<CodeClass>("Entity");
        Assert.NotNull(entityClass);
        CodeProperty? atType = entityClass.FindChildByName<CodeProperty>("Type");
        Assert.NotNull(atType);
        Assert.Equal("@type", atType.WireName);
        CodeClass? subtypeClass = codeModel.FindChildByName<CodeClass>("Subtype");
        Assert.NotNull(subtypeClass);
        CodeProperty? type = subtypeClass.FindChildByName<CodeProperty>("SubtypeType");
        Assert.NotNull(type);
        Assert.Equal("type", type.WireName);
        Assert.Equal("subtypeType", type.Name);
    }
    [Fact]
    public async Task CleanupSymbolNameDoesNotCauseNameConflictsInQueryParametersAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: Example
  description: Example
  version: 1.0.1
servers:
  - url: https://example.org
paths:
  /directoryObject:
    get:
      parameters:
        - name: $select
          in: query
          schema:
            type: string
        - name: select
          in: query
          schema:
            type: number
            format: int64
      responses:
        '200':
          content:
            application/json:
              schema:
                type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? parametersClass = codeModel.FindChildByName<CodeClass>("directoryObjectRequestBuilderGetQueryParameters");
        Assert.NotNull(parametersClass);
        CodeProperty? dollarSelect = parametersClass.FindChildByName<CodeProperty>("Select");
        Assert.NotNull(dollarSelect);
        Assert.Equal("%24select", dollarSelect.WireName);
        Assert.Equal("string", dollarSelect.Type.Name);
        CodeProperty? select = parametersClass.FindChildByName<CodeProperty>("select0");
        Assert.NotNull(select);
        Assert.Equal("select", select.WireName);
        Assert.Equal("int64", select.Type.Name);
    }
    [Fact]
    public async Task SupportsMultiPartFormAsRequestBodyWithDefaultMimeTypesAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: Example
  description: Example
  version: 1.0.1
servers:
  - url: https://example.org
paths:
  /directoryObject:
    post:
      requestBody:
        content:
          text/csv:
            schema:
              type: object
              properties:
                file:
                  type: string,
                  format: binary
            encoding:
              file:
                style: form
          multipart/form-data:
            schema:
              type: object
              properties:
                id:
                  type: string
                  format: uuid
                address:
                  $ref: '#/components/schemas/address'
                profileImage:
                  type: string
                  format: binary
            encoding:
              id:
                contentType: text/plain
              address:
                contentType: application/json
              profileImage:
                contentType: image/png
        responses:
          '204':
            content:
              application/json:
                schema:
                  type: string
components:
  schemas:
    address:
      type: object
      properties:
        street:
          type: string
        city:
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.NotNull(codeModel);
        CodeClass? rbClass = codeModel.FindChildByName<CodeClass>("directoryObjectRequestBuilder");
        Assert.NotNull(rbClass);
        CodeMethod? postMethod = rbClass.FindChildByName<CodeMethod>("Post", false);
        Assert.NotNull(postMethod);
        CodeParameter? bodyParameter = postMethod.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.RequestBody));
        Assert.NotNull(bodyParameter);
        Assert.Equal("MultipartBody", bodyParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
        CodeClass? addressClass = codeModel.FindChildByName<CodeClass>("Address");
        Assert.NotNull(addressClass);
    }
    [Fact]
    public async Task SupportsMultiPartFormAsRequestBodyWithoutEncodingWithDefaultMimeTypesAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: Example
  description: Example
  version: 1.0.1
servers:
  - url: https://example.org
paths:
  /directoryObject:
    post:
      requestBody:
        content:
          multipart/form-data:
            schema:
              type: object
              properties:
                id:
                  type: string
                  format: uuid
                address:
                  $ref: '#/components/schemas/address'
                profileImage:
                  type: string
                  format: binary
        responses:
          '204':
            content:
              application/json:
                schema:
                  type: string
components:
  schemas:
    address:
      type: object
      properties:
        street:
          type: string
        city:
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.NotNull(codeModel);
        CodeClass? rbClass = codeModel.FindChildByName<CodeClass>("directoryObjectRequestBuilder");
        Assert.NotNull(rbClass);
        CodeMethod? postMethod = rbClass.FindChildByName<CodeMethod>("Post", false);
        Assert.NotNull(postMethod);
        CodeParameter? bodyParameter = postMethod.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.RequestBody));
        Assert.NotNull(bodyParameter);
        Assert.Equal("MultipartBody", bodyParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
        CodeClass? addressClass = codeModel.FindChildByName<CodeClass>("Address"); // json is structured, we generated a model
        Assert.NotNull(addressClass);
    }
    [Fact]
    public async Task SupportsMultiPartFormAsRequestBodyWithoutEncodingWithDefaultMimeTypesAsyncWithNonDefaultMimeTypesAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: Example
  description: Example
  version: 1.0.1
servers:
  - url: https://example.org
paths:
  /directoryObject:
    post:
      requestBody:
        content:
          multipart/form-data:
            schema:
              type: object
              properties:
                id:
                  type: string
                  format: uuid
                address:
                  $ref: '#/components/schemas/address'
                profileImage:
                  type: string
                  format: binary
        responses:
          '204':
            content:
              application/json:
                schema:
                  type: string
components:
  schemas:
    address:
      type: object
      properties:
        street:
          type: string
        city:
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false, StructuredMimeTypes = ["multipart/form-data;q=1"] }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.NotNull(codeModel);
        CodeClass? rbClass = codeModel.FindChildByName<CodeClass>("directoryObjectRequestBuilder");
        Assert.NotNull(rbClass);
        CodeMethod? postMethod = rbClass.FindChildByName<CodeMethod>("Post", false);
        Assert.NotNull(postMethod);
        CodeParameter? bodyParameter = postMethod.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.RequestBody));
        Assert.NotNull(bodyParameter);
        Assert.Equal("MultipartBody", bodyParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Null(codeModel.FindChildByName<CodeClass>("Address")); // json is not structured so we didn't generate a model for the address
    }
    [Fact]
    public async Task SupportsMultipleContentTypesAsRequestBodyWithDefaultMimeTypesAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: Example
  description: Example
  version: 1.0.1
servers:
  - url: https://example.org
paths:
  /directoryObject:
    post:
      requestBody:
        content:
          multipart/form-data:
            schema:
              type: object
              properties:
                id:
                  type: string
                  format: uuid
                address:
                  $ref: '#/components/schemas/address'
                profileImage:
                  type: string
                  format: binary
          application/json:
            schema:
              type: object
              properties:
                id:
                  type: string
                  format: uuid
                address:
                  $ref: '#/components/schemas/address'
                profileImage:
                  type: string
                  format: binary
        responses:
          '204':
            content:
              application/json:
                schema:
                  type: string
components:
  schemas:
    address:
      type: object
      properties:
        street:
          type: string
        city:
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.NotNull(codeModel);
        CodeClass? rbClass = codeModel.FindChildByName<CodeClass>("directoryObjectRequestBuilder");
        Assert.NotNull(rbClass);
        CodeMethod? postMethod = rbClass.FindChildByName<CodeMethod>("Post", false);
        Assert.NotNull(postMethod);
        CodeParameter? bodyParameter = postMethod.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.RequestBody));
        Assert.NotNull(bodyParameter);
        Assert.Equal("directoryObjectPostRequestBody", bodyParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
        CodeClass? addressClass = codeModel.FindChildByName<CodeClass>("Address");
        Assert.NotNull(addressClass);
    }
    [Fact]
    public async Task SupportsMultipleContentTypesAsRequestBodyWithMultipartPriorityNoEncodingAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: Example
  description: Example
  version: 1.0.1
servers:
  - url: https://example.org
paths:
  /directoryObject:
    post:
      requestBody:
        content:
          multipart/form-data:
            schema:
              type: object
              properties:
                id:
                  type: string
                  format: uuid
                address:
                  $ref: '#/components/schemas/address'
                profileImage:
                  type: string
                  format: binary
          application/json:
            schema:
              type: object
              properties:
                id:
                  type: string
                  format: uuid
                address:
                  $ref: '#/components/schemas/address'
                profileImage:
                  type: string
                  format: binary
        responses:
          '204':
            content:
              application/json:
                schema:
                  type: string
components:
  schemas:
    address:
      type: object
      properties:
        street:
          type: string
        city:
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false, StructuredMimeTypes = ["multipart/form-data;q=1", "application/json;q=0.1"] }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.NotNull(codeModel);
        CodeClass? rbClass = codeModel.FindChildByName<CodeClass>("directoryObjectRequestBuilder");
        Assert.NotNull(rbClass);
        CodeMethod? postMethod = rbClass.FindChildByName<CodeMethod>("Post", false);
        Assert.NotNull(postMethod);
        CodeParameter? bodyParameter = postMethod.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.RequestBody));
        Assert.NotNull(bodyParameter);
        Assert.Equal("directoryObjectPostRequestBody", bodyParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
        CodeClass? addressClass = codeModel.FindChildByName<CodeClass>("Address");
        Assert.NotNull(addressClass);
    }
    [Fact]
    public async Task SupportsMultipleContentTypesAsRequestBodyWithMultipartPriorityAndEncodingAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: Example
  description: Example
  version: 1.0.1
servers:
  - url: https://example.org
paths:
  /directoryObject:
    post:
      requestBody:
        content:
          multipart/form-data:
            schema:
              type: object
              properties:
                id:
                  type: string
                  format: uuid
                address:
                  $ref: '#/components/schemas/address'
                profileImage:
                  type: string
                  format: binary
            encoding:
              id:
                contentType: text/plain
              address:
                contentType: application/json
              profileImage:
                contentType: image/png
          application/json:
            schema:
              type: object
              properties:
                id:
                  type: string
                  format: uuid
                address:
                  $ref: '#/components/schemas/address'
                profileImage:
                  type: string
                  format: binary
        responses:
          '204':
            content:
              application/json:
                schema:
                  type: string
components:
  schemas:
    address:
      type: object
      properties:
        street:
          type: string
        city:
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false, StructuredMimeTypes = ["multipart/form-data;q=1", "application/json;q=0.1"] }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.NotNull(codeModel);
        CodeClass? rbClass = codeModel.FindChildByName<CodeClass>("directoryObjectRequestBuilder");
        Assert.NotNull(rbClass);
        CodeMethod? postMethod = rbClass.FindChildByName<CodeMethod>("Post", false);
        Assert.NotNull(postMethod);
        CodeParameter? bodyParameter = postMethod.Parameters.FirstOrDefault(static x => x.IsOfKind(CodeParameterKind.RequestBody));
        Assert.NotNull(bodyParameter);
        Assert.Equal("MultipartBody", bodyParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
        CodeClass? addressClass = codeModel.FindChildByName<CodeClass>("Address");
        Assert.NotNull(addressClass);
    }
    [Fact]
    public async Task ComplexInheritanceStructuresAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: Broken inheritance
  version: '1'
servers:
- url: http://localhost
paths:
  '/groupclassification':
    get:
      summary: Example
      description: Example
      responses:
        '200':
          description: default response
          content:
            application/vnd.topicus.keyhub+json;version=latest:
              schema:
                '$ref': '#/components/schemas/group.GroupClassification'
components:
  schemas:
    Linkable:
      required:
      - '$type'
      type: object
      properties:
        '$type':
          type: string
      discriminator:
        propertyName: '$type'
        mapping:
          group.GroupPrimer: '#/components/schemas/group.GroupPrimer'
          group.GroupClassificationPrimer: '#/components/schemas/group.GroupClassificationPrimer'
          group.GroupClassification: '#/components/schemas/group.GroupClassification'
    group.GroupPrimer:
      allOf:
      - '$ref': '#/components/schemas/Linkable'
      - type: object
        properties:
          markers:
            '$ref': '#/components/schemas/mark.ItemMarkers'
    NonLinkable:
      required:
      - '$type'
      type: object
      properties:
        '$type':
          type: string
      discriminator:
        propertyName: '$type'
        mapping:
          mark.ItemMarkers: '#/components/schemas/mark.ItemMarkers'
          group.GroupsAuditStats: '#/components/schemas/group.GroupsAuditStats'
    mark.ItemMarkers:
      allOf:
      - '$ref': '#/components/schemas/NonLinkable'
      - type: object
    group.GroupClassificationPrimer:
      allOf:
      - '$ref': '#/components/schemas/Linkable'
      - required:
        - '$type'
        - name
        type: object
        properties:
          '$type':
            type: string
          name:
            type: string
        discriminator:
          propertyName: '$type'
          mapping:
            group.GroupClassification: '#/components/schemas/group.GroupClassification'
    group.GroupClassification:
      allOf:
      - '$ref': '#/components/schemas/group.GroupClassificationPrimer'
      - type: object
        properties:
          description:
            type: string
    group.GroupsAuditStats:
      allOf:
      - '$ref': '#/components/schemas/NonLinkable'
      - type: object
        properties:
          classification:
            '$ref': '#/components/schemas/group.GroupClassification'");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.NotNull(codeModel.FindChildByName<CodeClass>("Linkable"));
        CodeClass? classificationClass = codeModel.FindChildByName<CodeClass>("GroupClassification");
        Assert.NotNull(classificationClass);
        Assert.Single(classificationClass.Properties, static x => x.Name.Equals("description", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(classificationClass);
        CodeClass? classificationPrimerClass = codeModel.FindChildByName<CodeClass>("GroupClassificationPrimer");
        Assert.NotNull(classificationPrimerClass);
        Assert.Single(classificationPrimerClass.Properties, static x => x.Name.Equals("name", StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task InheritanceWithAllOfInBaseTypeAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /directoryObject:
    get:
      responses:
        '200':
          description: Example response
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.directoryObject'
components:
  schemas:
    microsoft.graph.directoryObject:
      type: object
      allOf:
        - title: 'directoryObject'
          required: ['@odata.type']
          type: 'object'
          properties:
            '@odata.type':
              type: 'string'
              default: '#microsoft.graph.directoryObject'
      discriminator:
        propertyName: '@odata.type'
        mapping:
          '#microsoft.graph.user': '#/components/schemas/microsoft.graph.user'
          '#microsoft.graph.group': '#/components/schemas/microsoft.graph.group'
    microsoft.graph.group:
      allOf:
        - '$ref': '#/components/schemas/microsoft.graph.directoryObject'
        - title: 'group'
          type: 'object'
          properties:
            groupprop:
              type: 'string'");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.NotNull(codeModel.FindChildByName<CodeClass>("Group"));
    }
    [Fact]
    public async Task InlineSchemaWithSingleAllOfReferenceAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /user:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.user'
  /group/members:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.member'
components:
  schemas:
    microsoft.graph.directoryObject:
      required: ['@odata.type']
      properties:
        '@odata.type':
          type: 'string'
          default: '#microsoft.graph.directoryObject'
      discriminator:
        propertyName: '@odata.type'
        mapping:
          '#microsoft.graph.group': '#/components/schemas/microsoft.graph.group'
    microsoft.graph.group:
      allOf:
        - '$ref': '#/components/schemas/microsoft.graph.directoryObject'
        - title: 'group'
          type: 'object'
          properties:
            groupprop:
              type: 'string'
    microsoft.graph.member:
      type: 'object'
      properties:
        group:
          allOf:
            - '$ref': '#/components/schemas/microsoft.graph.group'
    microsoft.graph.user:
      properties:
        groups:
          type: array
          items:
            - '$ref': '#/components/schemas/microsoft.graph.group'");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? memberClass = codeModel.FindChildByName<CodeClass>("member");
        Assert.NotNull(memberClass);
        Assert.Equal(2, memberClass.Properties.Count());// single prop plus additionalData
        CodeProperty? memberProperty = memberClass.Properties.FirstOrDefault(static x => x.Name.Equals("group", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(memberProperty);
        Assert.Equal("group", memberProperty.Type.Name);
        Assert.Null(memberClass.StartBlock.Inherits);//no base
        CodeClass? userClass = codeModel.FindChildByName<CodeClass>("user");
        Assert.NotNull(userClass);
        Assert.Equal(2, userClass.Properties.Count());// single prop plus additionalData
        Assert.Null(userClass.StartBlock.Inherits);//no base
        CodeClass? inlinedClassThatIsDuplicate = codeModel.FindChildByName<CodeClass>("member_group");
        Assert.Null(inlinedClassThatIsDuplicate);//no duplicate
        CodeNamespace? modelsNamespace = codeModel.FindChildByName<CodeNamespace>("ApiSdk.models.microsoft.graph");
        Assert.NotNull(modelsNamespace);
        Assert.Equal(4, modelsNamespace.Classes.Count());// only 4 classes for user, member, group and directoryObject
    }
    [Fact]
    public async Task InheritanceWithAllOfWith3Parts3SchemaChildClassAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.directoryObject'
  /group:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.group'
components:
  schemas:
    microsoft.graph.directoryObject:
      required: ['@odata.type']
      properties:
        '@odata.type':
          type: 'string'
          default: '#microsoft.graph.directoryObject'
      discriminator:
        propertyName: '@odata.type'
        mapping:
          '#microsoft.graph.group': '#/components/schemas/microsoft.graph.group'
    microsoft.graph.groupFacet1:
      properties:
        facetprop1:
          type: 'string'
    microsoft.graph.groupFacet2:
      properties:
        facetprop2:
          type: 'string'
    microsoft.graph.group:
      title: 'group'
      allOf:
        - '$ref': '#/components/schemas/microsoft.graph.directoryObject'
        - '$ref': '#/components/schemas/microsoft.graph.groupFacet1'
        - '$ref': '#/components/schemas/microsoft.graph.groupFacet2'");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? directoryObjectClass = codeModel.FindChildByName<CodeClass>("DirectoryObject");
        Assert.NotNull(directoryObjectClass);
        Assert.Null(directoryObjectClass.StartBlock.Inherits);
        Assert.NotNull(directoryObjectClass);
        CodeClass? groupClass = codeModel.FindChildByName<CodeClass>("Group");
        Assert.NotNull(groupClass);
        Assert.Equal(4, groupClass.Properties.Count());
        Assert.Null(groupClass.StartBlock.Inherits);
        Assert.Single(groupClass.Properties, static x => x.Kind is CodePropertyKind.AdditionalData);
        Assert.Single(groupClass.Properties, static x => x.Name.Equals("oDataType", StringComparison.OrdinalIgnoreCase));
        Assert.Single(groupClass.Properties, static x => x.Name.Equals("facetprop1", StringComparison.OrdinalIgnoreCase));
        Assert.Single(groupClass.Properties, static x => x.Name.Equals("facetprop2", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task InheritanceWithAllOfBaseClassNoAdditionalPropertiesAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.directoryResult'
components:
  schemas:
    microsoft.graph.directoryResult:
      required: ['fstype']
      oneOf:
        - $ref: '#/components/schemas/microsoft.graph.file'
        - $ref: '#/components/schemas/microsoft.graph.folder'
        - $ref: '#/components/schemas/microsoft.graph.link'
      properties:
        fstype:
          type: string
      discriminator:
        propertyName: 'fstype'
        mapping:
          'file': '#/components/schemas/microsoft.graph.file'
          'folder': '#/components/schemas/microsoft.graph.folder'
          'link': '#/components/schemas/microsoft.graph.link'
    microsoft.graph.baseDirectoryObject:
      properties:
        path:
          type: string
    microsoft.graph.file:
      allOf:
        - '$ref': '#/components/schemas/microsoft.graph.baseDirectoryObject'
    microsoft.graph.folder:
      allOf:
        - '$ref': '#/components/schemas/microsoft.graph.baseDirectoryObject'
    microsoft.graph.link:
      allOf:
        - '$ref': '#/components/schemas/microsoft.graph.baseDirectoryObject'
      properties:
        target:
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);

        // Verify that all three classes referenced by the discriminator inherit from baseDirectoryObject
        CodeClass? folder = codeModel.FindChildByName<CodeClass>("Folder");
        Assert.NotNull(folder);
        Assert.NotNull(folder.StartBlock.Inherits);
        Assert.Equal("baseDirectoryObject", folder.StartBlock.Inherits.Name);

        CodeClass? file = codeModel.FindChildByName<CodeClass>("File");
        Assert.NotNull(file);
        Assert.NotNull(file.StartBlock.Inherits);
        Assert.Equal("baseDirectoryObject", file.StartBlock.Inherits.Name);

        CodeClass? link = codeModel.FindChildByName<CodeClass>("Link");
        Assert.NotNull(link);
        Assert.NotNull(link.StartBlock.Inherits);
        Assert.Equal("baseDirectoryObject", link.StartBlock.Inherits.Name);
    }

    [Fact]
    public async Task ExclusiveUnionSingleEntriesMergingAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(
    """
openapi: 3.0.0
info:
  title: "Generator not generating oneOf if the containing schema has type: object"
  version: "1.0.0"
servers:
  - url: https://mytodos.doesnotexist/
paths:
  /uses-components:
    post:
      description: Return something
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/UsesComponents"
      requestBody:
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/UsesComponents"
components:
  schemas:
    ExampleWithSingleOneOfWithTypeObject:
      type: object
      oneOf:
        - $ref: "#/components/schemas/Component1"
      discriminator:
        propertyName: objectType
    ExampleWithSingleOneOfWithoutTypeObject:
      oneOf:
        - $ref: "#/components/schemas/Component2"
      discriminator:
        propertyName: objectType

    UsesComponents:
      type: object
      properties:
        component_with_single_oneof_with_type_object:
          $ref: "#/components/schemas/ExampleWithSingleOneOfWithTypeObject"
        component_with_single_oneof_without_type_object:
          $ref: "#/components/schemas/ExampleWithSingleOneOfWithoutTypeObject"

    Component1:
      type: object
      required:
        - objectType
      properties:
        objectType:
          type: string
        one:
          type: string

    Component2:
      type: object
      required:
        - objectType
      properties:
        objectType:
          type: string
        two:
          type: string
""");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);

        // Verify that all three classes referenced by the discriminator inherit from baseDirectoryObject
        CodeClass? withObjectClass = codeModel.FindChildByName<CodeClass>("ExampleWithSingleOneOfWithTypeObject");
        Assert.NotNull(withObjectClass);
        CodeProperty? oneProperty = withObjectClass.FindChildByName<CodeProperty>("one", false);
        Assert.NotNull(oneProperty);

        CodeClass? withoutObjectClass = codeModel.FindChildByName<CodeClass>("Component2");
        Assert.NotNull(withoutObjectClass);
        CodeProperty? twoProperty = withoutObjectClass.FindChildByName<CodeProperty>("two", false);
        Assert.NotNull(twoProperty);
    }

    [Fact]
    public async Task ExclusiveUnionInheritanceEntriesMergingAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(
    """
openapi: 3.0.0
info:
  title: "Generator not generating oneOf if the containing schema has type: object"
  version: "1.0.0"
servers:
  - url: https://mytodos.doesnotexist/
paths:
  /uses-components:
    post:
      description: Return something
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/UsesComponents"
      requestBody:
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/UsesComponents"
components:
  schemas:
    ExampleWithSingleOneOfWithTypeObject:
      description: "ExampleWithSingleOneOfWithTypeObject"
      type: object
      oneOf:
        - $ref: "#/components/schemas/Component1"
      discriminator:
        propertyName: objectType

    ExampleWithSingleOneOfWithoutTypeObject:
      description: "ExampleWithSingleOneOfWithoutTypeObject"
      oneOf:
        - $ref: "#/components/schemas/Component2"
      discriminator:
        propertyName: objectType

    UsesComponents:
      type: object
      properties:
        component_with_single_oneof_with_type_object:
          $ref: "#/components/schemas/ExampleWithSingleOneOfWithTypeObject"
        component_with_single_oneof_without_type_object:
          $ref: "#/components/schemas/ExampleWithSingleOneOfWithoutTypeObject"

    ComponentCommon:
      description: "ComponentCommon"
      type: object
      required:
        - objectType
      properties:
        objectType:
          type: string
        common:
          type: string

    Component1:
      description: "Component1"
      type: object
      allOf:
        - $ref: "#/components/schemas/ComponentCommon"
        - type: object
          description: "Component1Inner"
          properties:
            one:
              type: string
      properties:
        anotherOne:
          type: string

    Component2:
      description: "Component2"
      type: object
      allOf:
        - $ref: "#/components/schemas/ComponentCommon"
        - type: object
          description: "Component2Inner"
          properties:
            two:
              type: string
      properties:
        anotherTwo:
          type: string
""");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);

        // Verify that all three classes referenced by the discriminator inherit from baseDirectoryObject
        CodeClass? withObjectClass = codeModel.FindChildByName<CodeClass>("ExampleWithSingleOneOfWithTypeObject");
        Assert.NotNull(withObjectClass);
        // ExampleWithSingleOneOfWithTypeObject inherits from ComponentCommon
        Assert.Equal("ComponentCommon", withObjectClass.BaseClass?.Name);
        CodeProperty? withObjectClassOneProperty = withObjectClass.FindChildByName<CodeProperty>("one", false);
        Assert.NotNull(withObjectClassOneProperty);
        CodeProperty? withObjectClassAnotherOneProperty = withObjectClass.FindChildByName<CodeProperty>("anotherOne", false);
        Assert.NotNull(withObjectClassAnotherOneProperty);

        CodeClass? withoutObjectClass = codeModel.FindChildByName<CodeClass>("Component2");
        Assert.NotNull(withoutObjectClass);
        // Component2 inherits from ComponentCommon
        Assert.Equal("ComponentCommon", withoutObjectClass.BaseClass?.Name);
        CodeProperty? withoutObjectClassTwoProperty = withoutObjectClass.FindChildByName<CodeProperty>("two", false);
        Assert.NotNull(withoutObjectClassTwoProperty);
        CodeProperty? withoutObjectClassClassAnotherTwoProperty = withoutObjectClass.FindChildByName<CodeProperty>("anotherTwo", false);
        Assert.NotNull(withoutObjectClassClassAnotherTwoProperty);
    }

    [Fact]
    public async Task ExclusiveUnionIntersectionEntriesMergingAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(
    """
openapi: 3.0.0
info:
  title: "Generator not generating oneOf if the containing schema has type: object"
  version: "1.0.0"
servers:
  - url: https://mytodos.doesnotexist/
paths:
  /uses-components:
    post:
      description: Return something
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/UsesComponents"
      requestBody:
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/UsesComponents"
components:
  schemas:
    ExampleWithSingleOneOfWithTypeObject:
      description: "ExampleWithSingleOneOfWithTypeObject"
      type: object
      oneOf:
        - $ref: "#/components/schemas/Component1"
      discriminator:
        propertyName: objectType

    ExampleWithSingleOneOfWithoutTypeObject:
      description: "ExampleWithSingleOneOfWithoutTypeObject"
      oneOf:
        - $ref: "#/components/schemas/Component2"
      discriminator:
        propertyName: objectType

    UsesComponents:
      type: object
      properties:
        component_with_single_oneof_with_type_object:
          $ref: "#/components/schemas/ExampleWithSingleOneOfWithTypeObject"
        component_with_single_oneof_without_type_object:
          $ref: "#/components/schemas/ExampleWithSingleOneOfWithoutTypeObject"

    ComponentCommon:
      description: "ComponentCommon"
      type: object
      required:
        - objectType
      properties:
        objectType:
          type: string
        common:
          type: string

    ComponentCommon2:
      description: "ComponentCommon2"
      type: object
      properties:
        common2:
          type: string

    Component1:
      description: "Component1"
      type: object
      allOf:
        - $ref: "#/components/schemas/ComponentCommon"
        - $ref: "#/components/schemas/ComponentCommon2"
        - type: object
          description: "Component1Self"
          properties:
            one:
              type: string
      properties:
        anotherOne:
          type: string

    Component2:
      description: "Component2"
      type: object
      required:
        - objectType
      allOf:
        - $ref: "#/components/schemas/ComponentCommon"
        - $ref: "#/components/schemas/ComponentCommon2"
        - type: object
          description: "Component2Self"
          properties:
            two:
              type: string
      properties:
        anotherTwo:
          type: string
""");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);

        // Verify both scenarios have all the properties available from all schemas
        CodeClass? withObjectClass = codeModel.FindChildByName<CodeClass>("ExampleWithSingleOneOfWithTypeObject");
        Assert.NotNull(withObjectClass);
        CodeProperty? withObjectClassOneProperty = withObjectClass.FindChildByName<CodeProperty>("one", false);
        Assert.NotNull(withObjectClassOneProperty);
        CodeProperty? withObjectClassCommonProperty = withObjectClass.FindChildByName<CodeProperty>("common", false);
        Assert.NotNull(withObjectClassCommonProperty);
        CodeProperty? withObjectClassCommon2Property = withObjectClass.FindChildByName<CodeProperty>("common2", false);
        Assert.NotNull(withObjectClassCommon2Property);
        CodeProperty? withObjectClassObjectTypeProperty = withObjectClass.FindChildByName<CodeProperty>("objectType", false);
        Assert.NotNull(withObjectClassObjectTypeProperty);
        CodeProperty? withObjectClassAnotherOneProperty = withObjectClass.FindChildByName<CodeProperty>("anotherOne", false);
        Assert.NotNull(withObjectClassAnotherOneProperty);

        CodeClass? withoutObjectClass = codeModel.FindChildByName<CodeClass>("Component2");
        Assert.NotNull(withoutObjectClass);
        CodeProperty? withoutObjectClassTwoProperty = withoutObjectClass.FindChildByName<CodeProperty>("two", false);
        Assert.NotNull(withoutObjectClassTwoProperty);
        CodeProperty? withoutObjectClassCommonProperty = withoutObjectClass.FindChildByName<CodeProperty>("common", false);
        Assert.NotNull(withoutObjectClassCommonProperty);
        CodeProperty? withoutObjectClassCommon2Property = withoutObjectClass.FindChildByName<CodeProperty>("common2", false);
        Assert.NotNull(withoutObjectClassCommon2Property);
        CodeProperty? withoutObjectClassObjectTypeProperty = withoutObjectClass.FindChildByName<CodeProperty>("objectType", false);
        Assert.NotNull(withoutObjectClassObjectTypeProperty);
        CodeProperty? withoutObjectClassClassAnotherTwoProperty = withoutObjectClass.FindChildByName<CodeProperty>("anotherTwo", false);
        Assert.NotNull(withoutObjectClassClassAnotherTwoProperty);
    }

    [Fact]
    public async Task InclusiveUnionSingleEntriesMergingAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(
    """
openapi: 3.0.0
info:
  title: "Generator not generating anyOf if the containing schema has type: object"
  version: "1.0.0"
servers:
  - url: https://mytodos.doesnotexist/
paths:
  /uses-components:
    post:
      description: Return something
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/UsesComponents"
      requestBody:
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/UsesComponents"
components:
  schemas:
    ExampleWithSingleOneOfWithTypeObject:
      type: object
      anyOf:
        - $ref: "#/components/schemas/Component1"
      discriminator:
        propertyName: objectType
    ExampleWithSingleOneOfWithoutTypeObject:
      anyOf:
        - $ref: "#/components/schemas/Component2"
      discriminator:
        propertyName: objectType

    UsesComponents:
      type: object
      properties:
        component_with_single_oneof_with_type_object:
          $ref: "#/components/schemas/ExampleWithSingleOneOfWithTypeObject"
        component_with_single_oneof_without_type_object:
          $ref: "#/components/schemas/ExampleWithSingleOneOfWithoutTypeObject"

    Component1:
      type: object
      required:
        - objectType
      properties:
        objectType:
          type: string
        one:
          type: string

    Component2:
      type: object
      required:
        - objectType
      properties:
        objectType:
          type: string
        two:
          type: string
""");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);

        // Verify that all three classes referenced by the discriminator inherit from baseDirectoryObject
        CodeClass? withObjectClass = codeModel.FindChildByName<CodeClass>("ExampleWithSingleOneOfWithTypeObject");
        Assert.NotNull(withObjectClass);
        CodeProperty? oneProperty = withObjectClass.FindChildByName<CodeProperty>("one", false);
        Assert.NotNull(oneProperty);

        CodeClass? withoutObjectClass = codeModel.FindChildByName<CodeClass>("Component2");
        Assert.NotNull(withoutObjectClass);
        CodeProperty? twoProperty = withoutObjectClass.FindChildByName<CodeProperty>("two", false);
        Assert.NotNull(twoProperty);
    }

    [Fact]
    public async Task InclusiveUnionInheritanceEntriesMergingAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(
    """
openapi: 3.0.0
info:
  title: "Generator not generating oneOf if the containing schema has type: object"
  version: "1.0.0"
servers:
  - url: https://mytodos.doesnotexist/
paths:
  /uses-components:
    post:
      description: Return something
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/UsesComponents"
      requestBody:
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/UsesComponents"
components:
  schemas:
    ExampleWithSingleOneOfWithTypeObject:
      description: "ExampleWithSingleOneOfWithTypeObject"
      type: object
      anyOf:
        - $ref: "#/components/schemas/Component1"
      discriminator:
        propertyName: objectType

    ExampleWithSingleOneOfWithoutTypeObject:
      description: "ExampleWithSingleOneOfWithoutTypeObject"
      anyOf:
        - $ref: "#/components/schemas/Component2"
      discriminator:
        propertyName: objectType

    UsesComponents:
      type: object
      properties:
        component_with_single_oneof_with_type_object:
          $ref: "#/components/schemas/ExampleWithSingleOneOfWithTypeObject"
        component_with_single_oneof_without_type_object:
          $ref: "#/components/schemas/ExampleWithSingleOneOfWithoutTypeObject"

    ComponentCommon:
      description: "ComponentCommon"
      type: object
      required:
        - objectType
      properties:
        objectType:
          type: string
        common:
          type: string

    Component1:
      description: "Component1"
      type: object
      allOf:
        - $ref: "#/components/schemas/ComponentCommon"
        - type: object
          description: "Component1Inner"
          properties:
            one:
              type: string
      properties:
        anotherOne:
          type: string

    Component2:
      description: "Component2"
      type: object
      allOf:
        - $ref: "#/components/schemas/ComponentCommon"
        - type: object
          description: "Component2Inner"
          properties:
            two:
              type: string
      properties:
        anotherTwo:
          type: string
""");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);

        // Verify that all three classes referenced by the discriminator inherit from baseDirectoryObject
        CodeClass? withObjectClass = codeModel.FindChildByName<CodeClass>("ExampleWithSingleOneOfWithTypeObject");
        Assert.NotNull(withObjectClass);
        // ExampleWithSingleOneOfWithTypeObject inherits from ComponentCommon
        Assert.Equal("ComponentCommon", withObjectClass.BaseClass?.Name);
        CodeProperty? withObjectClassOneProperty = withObjectClass.FindChildByName<CodeProperty>("one", false);
        Assert.NotNull(withObjectClassOneProperty);
        CodeProperty? withObjectClassAnotherOneProperty = withObjectClass.FindChildByName<CodeProperty>("anotherOne", false);
        Assert.NotNull(withObjectClassAnotherOneProperty);

        CodeClass? withoutObjectClass = codeModel.FindChildByName<CodeClass>("Component2");
        Assert.NotNull(withoutObjectClass);
        // Component2 inherits from ComponentCommon
        Assert.Equal("ComponentCommon", withoutObjectClass.BaseClass?.Name);
        CodeProperty? withoutObjectClassTwoProperty = withoutObjectClass.FindChildByName<CodeProperty>("two", false);
        Assert.NotNull(withoutObjectClassTwoProperty);
        CodeProperty? withoutObjectClassClassAnotherTwoProperty = withoutObjectClass.FindChildByName<CodeProperty>("anotherTwo", false);
        Assert.NotNull(withoutObjectClassClassAnotherTwoProperty);
    }

    [Fact]
    public async Task InclusiveUnionIntersectionEntriesMergingAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(
    """
openapi: 3.0.0
info:
  title: "Generator not generating oneOf if the containing schema has type: object"
  version: "1.0.0"
servers:
  - url: https://mytodos.doesnotexist/
paths:
  /uses-components:
    post:
      description: Return something
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/UsesComponents"
      requestBody:
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/UsesComponents"
components:
  schemas:
    ExampleWithSingleOneOfWithTypeObject:
      description: "ExampleWithSingleOneOfWithTypeObject"
      type: object
      anyOf:
        - $ref: "#/components/schemas/Component1"
      discriminator:
        propertyName: objectType

    ExampleWithSingleOneOfWithoutTypeObject:
      description: "ExampleWithSingleOneOfWithoutTypeObject"
      anyOf:
        - $ref: "#/components/schemas/Component2"
      discriminator:
        propertyName: objectType

    UsesComponents:
      type: object
      properties:
        component_with_single_oneof_with_type_object:
          $ref: "#/components/schemas/ExampleWithSingleOneOfWithTypeObject"
        component_with_single_oneof_without_type_object:
          $ref: "#/components/schemas/ExampleWithSingleOneOfWithoutTypeObject"

    ComponentCommon:
      description: "ComponentCommon"
      type: object
      required:
        - objectType
      properties:
        objectType:
          type: string
        common:
          type: string

    ComponentCommon2:
      description: "ComponentCommon2"
      type: object
      properties:
        common2:
          type: string

    Component1:
      description: "Component1"
      type: object
      allOf:
        - $ref: "#/components/schemas/ComponentCommon"
        - $ref: "#/components/schemas/ComponentCommon2"
        - type: object
          description: "Component1Self"
          properties:
            one:
              type: string
      properties:
        anotherOne:
          type: string

    Component2:
      description: "Component2"
      type: object
      required:
        - objectType
      allOf:
        - $ref: "#/components/schemas/ComponentCommon"
        - $ref: "#/components/schemas/ComponentCommon2"
        - type: object
          description: "Component2Self"
          properties:
            two:
              type: string
      properties:
        anotherTwo:
          type: string
""");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);

        // Verify both scenarios have all the properties available from all schemas
        CodeClass? withObjectClass = codeModel.FindChildByName<CodeClass>("ExampleWithSingleOneOfWithTypeObject");
        Assert.NotNull(withObjectClass);
        CodeProperty? withObjectClassOneProperty = withObjectClass.FindChildByName<CodeProperty>("one", false);
        Assert.NotNull(withObjectClassOneProperty);
        CodeProperty? withObjectClassCommonProperty = withObjectClass.FindChildByName<CodeProperty>("common", false);
        Assert.NotNull(withObjectClassCommonProperty);
        CodeProperty? withObjectClassCommon2Property = withObjectClass.FindChildByName<CodeProperty>("common2", false);
        Assert.NotNull(withObjectClassCommon2Property);
        CodeProperty? withObjectClassObjectTypeProperty = withObjectClass.FindChildByName<CodeProperty>("objectType", false);
        Assert.NotNull(withObjectClassObjectTypeProperty);
        CodeProperty? withObjectClassAnotherOneProperty = withObjectClass.FindChildByName<CodeProperty>("anotherOne", false);
        Assert.NotNull(withObjectClassAnotherOneProperty);

        CodeClass? withoutObjectClass = codeModel.FindChildByName<CodeClass>("Component2");
        Assert.NotNull(withoutObjectClass);
        CodeProperty? withoutObjectClassTwoProperty = withoutObjectClass.FindChildByName<CodeProperty>("two", false);
        Assert.NotNull(withoutObjectClassTwoProperty);
        CodeProperty? withoutObjectClassCommonProperty = withoutObjectClass.FindChildByName<CodeProperty>("common", false);
        Assert.NotNull(withoutObjectClassCommonProperty);
        CodeProperty? withoutObjectClassCommon2Property = withoutObjectClass.FindChildByName<CodeProperty>("common2", false);
        Assert.NotNull(withoutObjectClassCommon2Property);
        CodeProperty? withoutObjectClassObjectTypeProperty = withoutObjectClass.FindChildByName<CodeProperty>("objectType", false);
        Assert.NotNull(withoutObjectClassObjectTypeProperty);
        CodeProperty? withoutObjectClassClassAnotherTwoProperty = withoutObjectClass.FindChildByName<CodeProperty>("anotherTwo", false);
        Assert.NotNull(withoutObjectClassClassAnotherTwoProperty);
    }

    [Fact]
    public async Task NestedIntersectionTypeAllOfAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(
    """
openapi: 3.0.3
info:
  title: Model Registry REST API
  version: v1alpha2
  description: REST API for Model Registry to create and manage ML model metadata
  license:
    name: Apache 2.0
    url: 'https://www.apache.org/licenses/LICENSE-2.0'
servers:
  - url: 'https://localhost:8080'
  - url: 'http://localhost:8080'
paths:
  /api/model_registry/v1alpha2/registered_models:
    summary: Path used to manage the list of registered models.
    description: >-
      The REST endpoint/path used to list and create zero or more `RegisteredModel` entities.  This path contains a `GET` and `POST` operation to perform the list and create tasks, respectively.
    get:
      responses:
        '200':
          $ref: '#/components/responses/RegisteredModelListResponse'
      summary: List All RegisteredModels
      description: Gets a list of all `RegisteredModel` entities.
components:
  schemas:
    BaseResource:
      type: object
      properties:
        id:
          format: int64
          description: Output only. The unique server generated id of the resource.
          type: number
          readOnly: true
      allOf:
        - $ref: '#/components/schemas/BaseResourceCreate'
    BaseResourceCreate:
      type: object
      properties:
        name:
          description: |-
            The client provided name of the artifact. This field is optional. If set,
            it must be unique among all the artifacts of the same artifact type within
            a database instance and cannot be changed once set.
          type: string
    BaseResourceList:
      required:
        - size
      type: object
      properties:
        size:
          format: int32
          description: Number of items in result list.
          type: integer
    RegisteredModel:
      description: A registered model in model registry. A registered model has ModelVersion children.
      allOf:
        - $ref: '#/components/schemas/BaseResource'
        - $ref: '#/components/schemas/RegisteredModelCreate'
    RegisteredModelCreate:
      description: A registered model in model registry. A registered model has ModelVersion children.
      allOf:
        - $ref: '#/components/schemas/BaseResourceCreate'
    RegisteredModelList:
      description: List of RegisteredModels.
      type: object
      allOf:
        - $ref: '#/components/schemas/BaseResourceList'
        - type: object
          properties:
            items:
              description: ''
              type: array
              items:
                $ref: '#/components/schemas/RegisteredModel'
              readOnly: false
  responses:
    RegisteredModelListResponse:
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/RegisteredModelList'
      description: A response containing a list of `RegisteredModel` entities.
""");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? registeredModelClass = codeModel.FindChildByName<CodeClass>("RegisteredModel");
        Assert.NotNull(registeredModelClass);
        Assert.Null(registeredModelClass.StartBlock.Inherits);
        Assert.Single(registeredModelClass.Properties, static x => x.Kind is CodePropertyKind.AdditionalData);
        Assert.Single(registeredModelClass.Properties, static x => x.Name.Equals("name", StringComparison.OrdinalIgnoreCase));
        Assert.Single(registeredModelClass.Properties, static x => x.Name.Equals("id", StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task InheritanceWithAllOfWith3Parts3SchemaParentClassAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.directoryObject'
  /group:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.group'
components:
  schemas:
    microsoft.graph.directoryObject:
      required: ['@odata.type']
      properties:
        '@odata.type':
          type: 'string'
          default: '#microsoft.graph.directoryObject'
      allOf:
        - '$ref': '#/components/schemas/microsoft.graph.directoryObjectFacet1'
        - '$ref': '#/components/schemas/microsoft.graph.directoryObjectFacet2'
      discriminator:
        propertyName: '@odata.type'
        mapping:
          '#microsoft.graph.group': '#/components/schemas/microsoft.graph.group'
    microsoft.graph.directoryObjectFacet1:
      properties:
        facetprop1:
          type: 'string'
    microsoft.graph.directoryObjectFacet2:
      properties:
        facetprop2:
          type: 'string'
    microsoft.graph.group:
      allOf:
        - '$ref': '#/components/schemas/microsoft.graph.directoryObject'
      properties:
        groupprop1:
          type: 'string'");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? directoryObjectClass = codeModel.FindChildByName<CodeClass>("DirectoryObject");
        Assert.NotNull(directoryObjectClass);
        Assert.Null(directoryObjectClass.StartBlock.Inherits);
        Assert.Single(directoryObjectClass.Properties, static x => x.Kind is CodePropertyKind.AdditionalData);
        Assert.Single(directoryObjectClass.Properties, static x => x.Name.Equals("oDataType", StringComparison.OrdinalIgnoreCase));
        Assert.Single(directoryObjectClass.Properties, static x => x.Name.Equals("facetprop1", StringComparison.OrdinalIgnoreCase));
        Assert.Single(directoryObjectClass.Properties, static x => x.Name.Equals("facetprop2", StringComparison.OrdinalIgnoreCase));
        CodeClass? groupClass = codeModel.FindChildByName<CodeClass>("Group");
        Assert.NotNull(groupClass);
        Assert.Single(groupClass.Properties);
        Assert.NotNull(groupClass.StartBlock.Inherits);
        Assert.DoesNotContain(groupClass.Properties, static x => x.Kind is CodePropertyKind.AdditionalData);
        Assert.DoesNotContain(groupClass.Properties, static x => x.Name.Equals("oDataType", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(groupClass.Properties, static x => x.Name.Equals("facetprop1", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(groupClass.Properties, static x => x.Name.Equals("facetprop2", StringComparison.OrdinalIgnoreCase));
        Assert.Single(groupClass.Properties, static x => x.Name.Equals("groupprop1", StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task InheritanceWithAllOfWith2Parts1Schema1InlineNoDiscriminatorAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /group:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.group'
components:
  schemas:
    microsoft.graph.directoryObject:
      title: 'directoryObject'
      required: ['@odata.type']
      type: 'object'
      properties:
        '@odata.type':
          type: 'string'
          default: '#microsoft.graph.directoryObject'
    microsoft.graph.group:
      type: 'object'
      allOf:
        - '$ref': '#/components/schemas/microsoft.graph.directoryObject'
        - title: 'group part 1'
          type: 'object'
          properties:
            groupprop1:
              type: 'string'");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? resultClass = codeModel.FindChildByName<CodeClass>("Group");
        Assert.NotNull(resultClass);
        Assert.Equal("directoryObject", resultClass.StartBlock.Inherits?.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Single(resultClass.Properties);
        Assert.Single(resultClass.Properties, static x => x.Name.Equals("groupprop1", StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task InheritanceWithAllOfWith1Part1SchemaAndPropertiesNoDiscriminatorAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /group:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.group'
components:
  schemas:
    microsoft.graph.directoryObject:
      title: 'directoryObject'
      required: ['@odata.type']
      type: 'object'
      properties:
        '@odata.type':
          type: 'string'
          default: '#microsoft.graph.directoryObject'
    microsoft.graph.group:
      allOf:
        - '$ref': '#/components/schemas/microsoft.graph.directoryObject'
      type: 'object'
      properties:
        groupprop1:
          type: 'string'");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? resultClass = codeModel.FindChildByName<CodeClass>("Group");
        Assert.NotNull(resultClass);
        Assert.Equal("directoryObject", resultClass.StartBlock.Inherits?.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Single(resultClass.Properties);
        Assert.Single(resultClass.Properties, static x => x.Name.Equals("groupprop1", StringComparison.OrdinalIgnoreCase));
    }
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task InheritanceWithAllOfWith3Parts1Schema2InlineAsync(bool reverseOrder)
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /directoryObject:
    get:
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/microsoft.graph.directoryObject'
components:
  schemas:
    microsoft.graph.directoryObject:
      required: ['@odata.type']
      properties:
        '@odata.type':
          type: 'string'
          default: '#microsoft.graph.directoryObject'
      discriminator:
        propertyName: '@odata.type'
        mapping:
          '#microsoft.graph.group': '#/components/schemas/microsoft.graph.group'
    microsoft.graph.group:
      allOf:"
       + (reverseOrder ? "" : @"
        - '$ref': '#/components/schemas/microsoft.graph.directoryObject'") + @"
        - properties:
            groupprop1:
              type: 'string'
        - properties:
            groupprop2:
              type: 'string'" + (!reverseOrder ? "" : @"
        - '$ref': '#/components/schemas/microsoft.graph.directoryObject'"));
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? resultClass = codeModel.FindChildByName<CodeClass>("Group");
        Assert.NotNull(resultClass);
        Assert.Equal("directoryObject", resultClass.StartBlock.Inherits?.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(2, resultClass.Properties.Count());
        Assert.DoesNotContain(resultClass.Properties, static x => x.Name.Equals("oDataType", StringComparison.OrdinalIgnoreCase));
        Assert.Single(resultClass.Properties, static x => x.Name.Equals("groupprop1", StringComparison.OrdinalIgnoreCase));
        Assert.Single(resultClass.Properties, static x => x.Name.Equals("groupprop2", StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task InheritanceWithoutObjectTypeHasAllPropertiesAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.3
servers:
  - url: 'https://example.com'
info:
  title: example
  version: 0.0.1
paths:
  /path:
    post:
      requestBody:
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/outerPayload'
      responses:
        '201':
          description: Created
          content:
            application/json:
              schema:
                type: string

components:
  schemas:
    outerPayload:
      allOf:
        - $ref: '#/components/schemas/innerPayload'
        - properties:
            someField:
              type: string
    innerPayload:
      properties:
        anotherField:
          type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeClass? outerPayloadClass = codeModel.FindChildByName<CodeClass>("outerPayload");
        Assert.NotNull(outerPayloadClass);
        Assert.Equal("innerPayload", outerPayloadClass.StartBlock.Inherits?.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Single(outerPayloadClass.Properties);
        Assert.Single(outerPayloadClass.Properties, static x => x.Name.Equals("someField", StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task EnumsWithNullableDoesNotResultInInlineTypeAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  '/communications/calls/{call-id}/reject':
    post:
      requestBody:
        description: Action parameters
        content:
          application/json:
            schema:
              type: object
              properties:
                reason:
                  anyOf:
                    - $ref: '#/components/schemas/microsoft.graph.rejectReason'
                    - type: object
                      nullable: true
                callbackUri:
                  type: string
                  nullable: true
        required: true
      responses:
        '204':
          description: Success,
components:
  schemas:
    microsoft.graph.rejectReason:
      title: rejectReason
      enum:
        - none
        - busy
        - forbidden
        - unknownFutureValue
      type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.NotNull(codeModel);
        CodeNamespace? requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.communications.calls.item.reject");
        Assert.NotNull(requestBuilderNS);
        CodeClass? requestBuilderClass = requestBuilderNS.FindChildByName<CodeClass>("RejectRequestBuilder", false);
        Assert.NotNull(requestBuilderClass);
        CodeEnum? reasonCandidate = requestBuilderNS.FindChildByName<CodeEnum>("RejectPostRequestBody_reason", false);
        Assert.Null(reasonCandidate);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("ApiSdk.Models");
        Assert.NotNull(modelsNS);
        CodeNamespace? graphModelsNS = modelsNS.FindNamespaceByName("ApiSdk.Models.Microsoft.Graph");
        Assert.NotNull(graphModelsNS);
        CodeEnum? rejectReasonEnum = graphModelsNS.FindChildByName<CodeEnum>("RejectReason", false);
        Assert.NotNull(rejectReasonEnum);
    }

    [Fact]
    public async Task EnumsWithNullableDoesNotResultInInlineTypeInReveredOrderAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  '/communications/calls/{call-id}/reject':
    post:
      requestBody:
        description: Action parameters
        content:
          application/json:
            schema:
              type: object
              properties:
                reason:
                  anyOf:
                    - type: object
                      nullable: true
                    - $ref: '#/components/schemas/microsoft.graph.rejectReason'
                callbackUri:
                  type: string
                  nullable: true
        required: true
      responses:
        '204':
          description: Success,
components:
  schemas:
    microsoft.graph.rejectReason:
      title: rejectReason
      enum:
        - none
        - busy
        - forbidden
        - unknownFutureValue
      type: string");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.NotNull(codeModel);
        CodeNamespace? requestBuilderNS = codeModel.FindNamespaceByName("ApiSdk.communications.calls.item.reject");
        Assert.NotNull(requestBuilderNS);
        CodeClass? requestBuilderClass = requestBuilderNS.FindChildByName<CodeClass>("RejectRequestBuilder", false);
        Assert.NotNull(requestBuilderClass);
        CodeEnum? reasonCandidate = requestBuilderNS.FindChildByName<CodeEnum>("RejectPostRequestBody_reason", false);
        Assert.Null(reasonCandidate);
        CodeNamespace? modelsNS = codeModel.FindNamespaceByName("ApiSdk.Models");
        Assert.NotNull(modelsNS);
        CodeNamespace? graphModelsNS = modelsNS.FindNamespaceByName("ApiSdk.Models.Microsoft.Graph");
        Assert.NotNull(graphModelsNS);
        CodeEnum? rejectReasonEnum = graphModelsNS.FindChildByName<CodeEnum>("RejectReason", false);
        Assert.NotNull(rejectReasonEnum);
    }

    [Fact]
    public async Task AnyTypeResponseAsync()
    {
        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(@"openapi: 3.0.1
info:
  title: The Jira Cloud platform REST API
externalDocs:
  description: Find out more about Atlassian products and services.
  url: http://www.atlassian.com
paths:
  /issueLink:
    post:
      tags:
        - Issue links
      summary: Create issue link
      operationId: linkIssues
      parameters: []
      requestBody:
        description: The issue link request.
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/LinkIssueRequestJsonBean'
        required: true
      responses:
        '201':
          description: Returned if the request is successful.
          content:
            application/json:
              schema: {}
        '400':
          description: no desc.
        '401':
          description: no desc.
        '404':
          description: no desc.
      deprecated: false
components:
  schemas:
    Comment:
      type: object
      properties:
        body:
          description: >-
            The comment text in [Atlassian Document
            Format](https://developer.atlassian.com/cloud/jira/platform/apis/document/structure/).
        created:
          type: string
          format: date-time
          readOnly: true
        id:
          type: string
          readOnly: true
        jsdAuthorCanSeeRequest:
          type: boolean
          readOnly: true
        jsdPublic:
          type: boolean
          readOnly: true
        renderedBody:
          type: string
          readOnly: true
        self:
          type: string
          description: The URL of the comment.
          readOnly: true
        updated:
          type: string
          description: The date and time at which the comment was updated last.
          format: date-time
          readOnly: true
      additionalProperties: true
      description: A comment.
    IssueLinkType:
      type: object
      properties:
        id:
          type: string
        inward:
          type: string
        name:
          type: string
        outward:
          type: string
        self:
          type: string
          format: uri
          readOnly: true
      additionalProperties: false
    LinkIssueRequestJsonBean:
      required:
        - inwardIssue
        - outwardIssue
        - type
      type: object
      properties:
        comment:
          $ref: '#/components/schemas/Comment'
        inwardIssue:
          $ref: '#/components/schemas/LinkedIssue'
        outwardIssue:
          $ref: '#/components/schemas/LinkedIssue'
        type:
          $ref: '#/components/schemas/IssueLinkType'
      additionalProperties: false
    LinkedIssue:
      type: object
      properties:
        fields:
          description: The fields associated with the issue.
          readOnly: true
        id:
          type: string
          description: The ID of an issue. Required if `key` isn't provided.
        key:
          type: string
          description: The key of an issue. Required if `id` isn't provided.
        self:
          type: string
          description: The URL of the issue.
          format: uri
          readOnly: true
      additionalProperties: false
      description: The ID or key of a linked issue.");
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Graph", OpenAPIFilePath = tempFilePath, IncludeAdditionalData = false }, _httpClient);
        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.NotNull(codeModel);
        CodeClass? rbClass = codeModel.FindChildByName<CodeClass>("issueLinkRequestBuilder");
        Assert.NotNull(rbClass);
        CodeMethod? postMethod = rbClass.FindChildByName<CodeMethod>("Post", false);
        Assert.NotNull(postMethod);
        CodeClass? linkIssueRequestJsonBeanClass = codeModel.FindChildByName<CodeClass>("LinkIssueRequestJsonBean");
        Assert.NotNull(linkIssueRequestJsonBeanClass);
    }

    [Fact]
    public async Task EnumArrayQueryParameterAsync()
    {
        const string schemaDocument = """
                     openapi: 3.0.2
                     info:
                       title: Enum
                       version: 1.0.0
                     paths:
                       /EnumQuery:
                         get:
                           parameters:
                             - name: enumValues
                               in: query
                               schema:
                                 type: array
                                 items:
                                   $ref: '#/components/schemas/EnumValue'
                             - name: enumValues2
                               in: query
                               schema:
                                 $ref: '#/components/schemas/EnumValue'
                           responses:
                             '200':
                               description: response
                               content:
                                 application/json:
                                   schema:
                                     $ref: '#/components/schemas/EnumObject'
                     components:
                       schemas:
                         EnumValue:
                           type: string
                           enum:
                             - Value1
                             - Value2
                             - Value3
                         EnumObject:
                           type: object
                           properties:
                             enumArray:
                               type: array
                               items:
                                 $ref: '#/components/schemas/EnumValue'
                     """;

        string tempFilePath = Path.GetTempFileName();
        await using Stream fs = await GetDocumentStreamAsync(schemaDocument);

        ReQuestyBuilder builder = new(
            NullLogger<ReQuestyBuilder>.Instance,
            new GenerationConfiguration
            {
                ClientClassName = "EnumTest",
                OpenAPIFilePath = tempFilePath,
                IncludeAdditionalData = false
            },
            _httpClient);

        OpenApiDocument? document = await builder.CreateOpenApiDocumentAsync(fs);
        Assert.NotNull(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.NotNull(codeModel);
        CodeClass? enumRequestBuilder = codeModel.FindChildByName<CodeClass>("EnumQueryRequestBuilder");
        Assert.NotNull(enumRequestBuilder);
        CodeClass? queryParameters = enumRequestBuilder.FindChildByName<CodeClass>("EnumQueryRequestBuilderGetQueryParameters");
        Assert.NotNull(queryParameters);

        Assert.Contains(queryParameters.Properties, p =>
            p.Type is
            {
                IsCollection: true,
                IsArray: true,
                CollectionKind: CodeTypeBase.CodeTypeCollectionKind.Array,
                Name: "EnumValue"
            });
    }
    [Fact]
    public void SupportsIncludeFilterAndExcludeWithOperation()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["directory/administrativeUnits"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        },
                        [NetHttpMethod.Post] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody
                            {
                                Content = new Dictionary<string, IOpenApiMediaType>()
                                {
                                    ["application/json"] = new OpenApiMediaType()
                                    {
                                        Schema = new OpenApiSchemaReference("myobject")
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["201"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
                ["directory/administrativeUnits/{id}"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        },
                        [NetHttpMethod.Patch] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody
                            {
                                Content = new Dictionary<string, IOpenApiMediaType>()
                                {
                                    ["application/json"] = new OpenApiMediaType()
                                    {
                                        Schema = new OpenApiSchemaReference("myobject")
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse()
                            }
                        },
                        [NetHttpMethod.Delete] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse()
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration
        {
            ClientClassName = "TestClient",
            ClientNamespaceName = "TestSdk",
            ApiRootUrl = "https://localhost",
            IncludePatterns = [
                "directory/administrativeUnits",
                "directory/administrativeUnits/**"
            ],
            ExcludePatterns =
            [
                "directory/administrativeUnits/**#DELETE"
            ]
        }, _httpClient);
        builder.FilterPathsByPatterns(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.Null(codeModel.FindNamespaceByName("TestSdk.groups"));
        CodeNamespace? administrativeUnitsNS = codeModel.FindNamespaceByName("TestSdk.directory.administrativeUnits");
        Assert.NotNull(administrativeUnitsNS);
        CodeClass? administrativeUnitsRS = administrativeUnitsNS.FindChildByName<CodeClass>("AdministrativeUnitsRequestBuilder");
        Assert.NotNull(administrativeUnitsRS);
        Assert.Single(administrativeUnitsRS.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Post);
        Assert.Single(administrativeUnitsRS.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Get);
        Assert.DoesNotContain(administrativeUnitsRS.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Put);
        CodeNamespace? administrativeUnitsItemsNS = codeModel.FindNamespaceByName("TestSdk.directory.administrativeUnits.item");
        Assert.NotNull(administrativeUnitsItemsNS);
        CodeClass? administrativeUnitItemsRS = administrativeUnitsItemsNS.FindChildByName<CodeClass>("AdministrativeUnitsItemRequestBuilder");
        Assert.NotNull(administrativeUnitItemsRS);
        Assert.Single(administrativeUnitItemsRS.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Get);
        Assert.Single(administrativeUnitItemsRS.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Patch);
        Assert.DoesNotContain(administrativeUnitItemsRS.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Delete);
    }
    [Fact]
    public void SupportsIncludeFilterAndExcludeWithOperationForSpecificPath()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["directory/administrativeUnits"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        },
                        [NetHttpMethod.Post] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody
                            {
                                Content = new Dictionary<string, IOpenApiMediaType>()
                                {
                                    ["application/json"] = new OpenApiMediaType()
                                    {
                                        Schema = new OpenApiSchemaReference("myobject")
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["201"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                },
                ["directory/administrativeUnits/{id}"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        },
                        [NetHttpMethod.Patch] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody
                            {
                                Content = new Dictionary<string, IOpenApiMediaType>()
                                {
                                    ["application/json"] = new OpenApiMediaType()
                                    {
                                        Schema = new OpenApiSchemaReference("myobject")
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse()
                            }
                        },
                        [NetHttpMethod.Delete] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse()
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(mockLogger.Object, new GenerationConfiguration
        {
            ClientClassName = "TestClient",
            ClientNamespaceName = "TestSdk",
            ApiRootUrl = "https://localhost",
            IncludePatterns = [
                "directory/administrativeUnits",
                "directory/administrativeUnits/**"
            ],
            ExcludePatterns =
            [
                "directory/administrativeUnits/{id}#DELETE"
            ]
        }, _httpClient);
        builder.FilterPathsByPatterns(document);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        Assert.Null(codeModel.FindNamespaceByName("TestSdk.groups"));
        CodeNamespace? administrativeUnitsNS = codeModel.FindNamespaceByName("TestSdk.directory.administrativeUnits");
        Assert.NotNull(administrativeUnitsNS);
        CodeClass? administrativeUnitsRS = administrativeUnitsNS.FindChildByName<CodeClass>("AdministrativeUnitsRequestBuilder");
        Assert.NotNull(administrativeUnitsRS);
        Assert.Single(administrativeUnitsRS.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Post);
        Assert.Single(administrativeUnitsRS.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Get);
        Assert.DoesNotContain(administrativeUnitsRS.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Put);
        CodeNamespace? administrativeUnitsItemsNS = codeModel.FindNamespaceByName("TestSdk.directory.administrativeUnits.item");
        Assert.NotNull(administrativeUnitsItemsNS);
        CodeClass? administrativeUnitItemsRS = administrativeUnitsItemsNS.FindChildByName<CodeClass>("AdministrativeUnitsItemRequestBuilder");
        Assert.NotNull(administrativeUnitItemsRS);
        Assert.Single(administrativeUnitItemsRS.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Get);
        Assert.Single(administrativeUnitItemsRS.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Patch);
        Assert.DoesNotContain(administrativeUnitItemsRS.Methods, static x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == Builder.CodeDOM.HttpMethod.Delete);
    }
    [Fact]
    public void CleansUpOperationIdAddsMissingOperationId()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["directory/administrativeUnits"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        },
                        [NetHttpMethod.Post] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody
                            {
                                Content = new Dictionary<string, IOpenApiMediaType>()
                                {
                                    ["application/json"] = new OpenApiMediaType()
                                    {
                                        Schema = new OpenApiSchemaReference("myobject")
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["201"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        ReQuestyBuilder.CleanupOperationIdForPlugins(document);
        List<KeyValuePair<NetHttpMethod, OpenApiOperation>> operations = document.Paths.SelectMany(path => path.Value.Operations!).ToList();
        foreach (KeyValuePair<NetHttpMethod, OpenApiOperation> path in operations)
        {
            Assert.False(string.IsNullOrEmpty(path.Value.OperationId)); //Assert that the operationId is not empty
            Assert.EndsWith(path.Key.ToString().ToLowerInvariant(), path.Value.OperationId);// assert that the operationId ends with the operation type
            Assert.Matches(OperationIdValidationRegex(), path.Value.OperationId); // assert that the operationId is clean an matches the regex
        }
        Assert.Equal("directory_administrativeunits_get", operations[0].Value.OperationId);
        Assert.Equal("directory_administrativeunits_post", operations[1].Value.OperationId);
    }

    [Theory]
    [InlineData("repos/{id}/", "repos/{*}/")] // normalish case
    [InlineData("repos/{id}", "repos/{*}")]// no trailing slash
    [InlineData("/repos/{id}", "/repos/{*}")]// no trailing slash(slash at beginning).
    [InlineData("repos/{id}/dependencies/{dep-id}", "repos/{*}/dependencies/{*}")]// multiple indexers
    [InlineData("/repos/{id}/dependencies/{dep-id}/", "/repos/{*}/dependencies/{*}/")]// multiple indexers(slash at beginning and end).
    [InlineData("/repos/{id}/dependencies/{dep-id}", "/repos/{*}/dependencies/{*}")]// multiple indexers(slash at beginning).
    [InlineData("repos/{id}/{dep-id}", "repos/{*}/{*}")]// indexers following each other.
    [InlineData("/repos/{id}/{dep-id}", "/repos/{*}/{*}")]// indexers following each other(slash at beginning).
    [InlineData("repos/msft", "repos/msft")]// no indexers
    [InlineData("/repos", "/repos")]// no indexers(slash at beginning).
    [InlineData("repos", "repos")]// no indexers
    public void ReplacesAllIndexesWithWildcard(string inputPath, string expectedGlob)
    {
        string resultGlob = ReQuestyBuilder.ReplaceAllIndexesWithWildcard(inputPath);
        Assert.Equal(expectedGlob, resultGlob);
    }

    [Fact]
    public void CleansUpOperationIdChangesOperationId()
    {
        OpenApiSchema myObjectSchema = new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema> {
                {
                    "name", new OpenApiSchema {
                        Type = JsonSchemaType.String,
                    }
                }
            },
        };
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["directory/administrativeUnits"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            },
                            OperationId = "GetAdministrativeUnits" // Nothing wrong with this operationId
                        },
                        [NetHttpMethod.Post] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody
                            {
                                Content = new Dictionary<string, IOpenApiMediaType>()
                                {
                                    ["application/json"] = new OpenApiMediaType()
                                    {
                                        Schema = new OpenApiSchemaReference("myobject")
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["201"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            },
                            OperationId = "PostAdministrativeUnits.With201-response" // operationId should be cleaned up
                        }
                    }
                },
                ["directory/adminstativeUnits/{unit-id}"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Get] = new OpenApiOperation
                        {
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, IOpenApiMediaType>()
                                    {
                                        ["application/json"] = new OpenApiMediaType()
                                        {
                                            Schema = new OpenApiSchemaReference("myobject")
                                        }
                                    }
                                },
                            },
                            // OperationId is missing
                        },
                    }
                }
            },
        };
        document.AddComponent("myobject", myObjectSchema);
        document.SetReferenceHostDocument();
        ReQuestyBuilder.CleanupOperationIdForPlugins(document);
        List<KeyValuePair<NetHttpMethod, OpenApiOperation>> operations = document.Paths.SelectMany(path => path.Value.Operations!).ToList();
        foreach (KeyValuePair<NetHttpMethod, OpenApiOperation> path in operations)
        {
            Assert.False(string.IsNullOrEmpty(path.Value.OperationId)); //Assert that the operationId is not empty
            Assert.Matches(OperationIdValidationRegex(), path.Value.OperationId); // assert that the operationId is clean an matches the regex
        }
        Assert.Equal("GetAdministrativeUnits", operations[0].Value.OperationId);
        Assert.Equal("PostAdministrativeUnits_With201_response", operations[1].Value.OperationId);
        Assert.Equal("directory_adminstativeunits_item_get", operations[2].Value.OperationId);
    }

    [GeneratedRegex(@"^[a-zA-Z0-9_]*$", RegexOptions.IgnoreCase | RegexOptions.Singleline, 2000)]
    private static partial Regex OperationIdValidationRegex();
}
