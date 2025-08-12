using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Moq;
using Xunit;
using NetHttpMethod = System.Net.Http.HttpMethod;

namespace ReQuesty.Builder.Tests;

public sealed class ContentTypeMappingTests : IDisposable
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
    private readonly HttpClient _httpClient = new();

    [InlineData("application/json", "206", true, "default", "myobject")]
    [InlineData("application/json", "206", false, "default", "binary")]
    [InlineData("application/json", "205", true, "default", "void")]
    [InlineData("application/json", "205", false, "default", "void")]
    [InlineData("application/json", "204", true, "default", "void")]
    [InlineData("application/json", "204", false, "default", "void")]
    [InlineData("application/json", "203", true, "default", "myobject")]
    [InlineData("application/json", "203", false, "default", "binary")]
    [InlineData("application/json", "202", true, "default", "myobject")]
    [InlineData("application/json", "202", false, "default", "void")]
    [InlineData("application/json", "201", true, "default", "myobject")]
    [InlineData("application/json", "201", false, "default", "void")]
    [InlineData("application/json", "200", true, "default", "myobject")]
    [InlineData("application/json", "200", false, "default", "binary")]
    [InlineData("application/json", "2XX", true, "default", "myobject")]
    [InlineData("application/json", "2XX", false, "default", "binary")]
    [InlineData("application/xml", "204", true, "default", "void")]
    [InlineData("application/xml", "204", false, "default", "void")]
    [InlineData("application/xml", "200", true, "default", "binary")] // MyObject when we support xml deserialization
    [InlineData("application/xml", "200", false, "default", "binary")]
    [InlineData("text/xml", "204", true, "default", "void")]
    [InlineData("text/xml", "204", false, "default", "void")]
    [InlineData("text/xml", "200", true, "default", "binary")] // MyObject when we support xml deserialization
    [InlineData("text/xml", "200", false, "default", "binary")]
    [InlineData("text/yaml", "204", true, "default", "void")]
    [InlineData("text/yaml", "204", false, "default", "void")]
    [InlineData("text/yaml", "200", true, "default", "binary")] // MyObject when we support xml deserialization
    [InlineData("text/yaml", "200", false, "default", "binary")]
    [InlineData("application/octet-stream", "204", true, "default", "void")]
    [InlineData("application/octet-stream", "204", false, "default", "void")]
    [InlineData("application/octet-stream", "200", true, "default", "binary")]
    [InlineData("application/octet-stream", "200", false, "default", "binary")]
    [InlineData("application/octet-stream", "302", false, "default", "binary")] // on a redirect to a binary content we generate a binary return type for download
    [InlineData("text/html", "204", true, "default", "void")]
    [InlineData("text/html", "204", false, "default", "void")]
    [InlineData("text/html", "200", true, "default", "binary")]
    [InlineData("text/html", "200", false, "default", "binary")]
    [InlineData("*/*", "204", true, "default", "void")]
    [InlineData("*/*", "204", false, "default", "void")]
    [InlineData("*/*", "200", true, "default", "binary")]
    [InlineData("*/*", "200", false, "default", "binary")]
    [InlineData("text/plain", "204", true, "default", "void")]
    [InlineData("text/plain", "204", false, "default", "void")]
    [InlineData("text/plain", "200", true, "default", "myobject")]
    [InlineData("text/plain", "200", false, "default", "string")]
    [InlineData("text/plain", "204", true, "application/json", "void")]
    [InlineData("text/plain", "204", false, "application/json", "void")]
    [InlineData("text/plain", "200", true, "application/json", "string")]
    [InlineData("text/plain", "200", false, "application/json", "string")]
    [InlineData("text/yaml", "204", true, "application/json", "void")]
    [InlineData("text/yaml", "204", false, "application/json", "void")]
    [InlineData("text/yaml", "200", true, "application/json", "binary")]
    [InlineData("text/yaml", "200", false, "application/json", "binary")]
    [Theory]
    public void GeneratesTheRightReturnTypeBasedOnContentAndStatus(string contentType, string statusCode, bool addModel, string acceptedContentType, string returnType)
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
                                [statusCode] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, OpenApiMediaType>()
                                    {
                                        [contentType] = new OpenApiMediaType
                                        {
                                            Schema = addModel ? new OpenApiSchemaReference("myobject") : null
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, IOpenApiSchema> {
                    {
                        "myobject", new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema> {
                                {
                                    "id", new OpenApiSchema {
                                        Type = JsonSchemaType.String,
                                    }
                                }
                            },
                        }
                    }
                }
            }
        };
        document.RegisterComponents();
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(
            mockLogger.Object,
            new GenerationConfiguration
            {
                ClientClassName = "TestClient",
                ClientNamespaceName = "TestSdk",
                ApiRootUrl = "https://localhost",
                StructuredMimeTypes = acceptedContentType.Equals("default", StringComparison.OrdinalIgnoreCase) ?
                                            new GenerationConfiguration().StructuredMimeTypes :
                                            [acceptedContentType]
            }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        CodeClass? rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        CodeMethod? executor = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executor);
        Assert.Equal(returnType, executor.ReturnType.Name);
    }
    [InlineData("application/json", true, "default", "myobject")]
    [InlineData("application/json", false, "default", "binary")]
    [InlineData("application/xml", false, "default", "binary")]
    [InlineData("application/xml", true, "default", "binary")] //MyObject when we support it
    [InlineData("text/xml", false, "default", "binary")]
    [InlineData("text/xml", true, "default", "binary")] //MyObject when we support it
    [InlineData("text/yaml", false, "default", "binary")]
    [InlineData("text/yaml", true, "default", "binary")] //MyObject when we support it
    [InlineData("application/octet-stream", true, "default", "binary")]
    [InlineData("application/octet-stream", false, "default", "binary")]
    [InlineData("text/html", true, "default", "binary")]
    [InlineData("text/html", false, "default", "binary")]
    [InlineData("*/*", true, "default", "binary")]
    [InlineData("*/*", false, "default", "binary")]
    [InlineData("text/plain", false, "default", "binary")]
    [InlineData("text/plain", true, "default", "myobject")]
    [InlineData("text/plain", true, "application/json", "binary")]
    [InlineData("text/plain", false, "application/json", "binary")]
    [InlineData("text/yaml", true, "application/json", "binary")]
    [InlineData("text/yaml", false, "application/json", "binary")]
    [Theory]
    public void GeneratesTheRightParameterTypeBasedOnContentAndStatus(string contentType, bool addModel, string acceptedContentType, string parameterType)
    {
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Post] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody
                            {
                                Content = new Dictionary<string, OpenApiMediaType>()
                                {
                                    [contentType] = new OpenApiMediaType
                                    {
                                        Schema = addModel ? new OpenApiSchemaReference("myobject") : null
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["204"] = new OpenApiResponse(),
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, IOpenApiSchema> {
                    {
                        "myobject", new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema> {
                                {
                                    "id", new OpenApiSchema {
                                        Type = JsonSchemaType.String,
                                    }
                                }
                            },
                        }
                    }
                }
            }
        };
        document.RegisterComponents();
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(
            mockLogger.Object,
            new GenerationConfiguration
            {
                ClientClassName = "TestClient",
                ClientNamespaceName = "TestSdk",
                ApiRootUrl = "https://localhost",
                StructuredMimeTypes = acceptedContentType.Equals("default", StringComparison.OrdinalIgnoreCase) ?
                                            new GenerationConfiguration().StructuredMimeTypes :
                                            [acceptedContentType]
            }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        CodeClass? rbClass = rbNS.Classes.FirstOrDefault(x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        CodeMethod? executor = rbClass.Methods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
        Assert.NotNull(executor);
        Assert.Equal(parameterType, executor.Parameters.OfKind(CodeParameterKind.RequestBody)!.Type.Name);
    }
    [Theory]
    [InlineData("application/json, text/plain", "application/json", "application/json", "text/plain")]
    [InlineData("application/json, text/plain, application/yaml", "application/json;q=0.8,application/yaml", "application/yaml,application/json;q=0.8", "text/plain")]
    [InlineData("*/*", "application/json;q=0.8", "*/*", "application/json;q=0.8")]
    [InlineData("application/json, */*", "application/json;q=0.8", "application/json;q=0.8", "*/*")]
    [InlineData("application/png, application/jpg", "application/json;q=0.8", "application/png, application/jpg", "application/json;q=0.8")]
    public void GeneratesTheRightAcceptHeaderBasedOnContentAndStatus(string contentMediaTypes, string structuredMimeTypes, string expectedAcceptHeader, string unexpectedMimeTypes)
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
                                ["200"] = new OpenApiResponse
                                {
                                    Content = contentMediaTypes.Split(',').Select(x => new
                                    {
                                        Key = x.Trim(),
                                        value = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchemaReference("myobject"),
                                        }
                                    }).ToDictionary(x => x.Key, x => x.value)
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, IOpenApiSchema> {
                    {
                        "myobject", new OpenApiSchema {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema> {
                                {
                                    "id", new OpenApiSchema {
                                        Type = JsonSchemaType.String,
                                    }
                                }
                            },
                        }
                    }
                }
            }
        };
        document.RegisterComponents();
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(
            mockLogger.Object,
            new GenerationConfiguration
            {
                ClientClassName = "TestClient",
                ClientNamespaceName = "TestSdk",
                ApiRootUrl = "https://localhost",
                StructuredMimeTypes = new(structuredMimeTypes.Split(',').Select(x => x.Trim()))
            }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        CodeClass? rbClass = rbNS.Classes.FirstOrDefault(static x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        CodeMethod? generator = rbClass.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.RequestGenerator));
        Assert.NotNull(generator);
        foreach (string header in expectedAcceptHeader.Split(','))
        {
            Assert.Contains(header.Trim(), generator.AcceptedResponseTypes);
        }

        foreach (string header in unexpectedMimeTypes.Split(','))
        {
            Assert.DoesNotContain(header.Trim(), generator.AcceptedResponseTypes);
        }
    }
    [Theory]
    [InlineData("application/json, text/plain", "application/json", "application/json", "text/plain")]
    [InlineData("application/json, text/plain, application/yaml", "application/json;q=0.8,application/yaml", "application/yaml,application/json;q=0.8", "text/plain")]
    [InlineData("*/*", "application/json;q=0.8", "", "application/json;q=0.8")]
    [InlineData("application/json, */*", "application/json;q=0.8", "application/json;q=0.8", "*/*")]
    [InlineData("application/png, application/jpg", "application/json;q=0.8", "", "application/json;q=0.8")]
    public void IncludeErrorsMediaTypeInAcceptHeader(string contentMediaTypes, string structuredMimeTypes, string expectedAcceptHeader, string unexpectedMimeTypes)
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
                                ["204"] = new OpenApiResponse(),
                                ["403"] = new OpenApiResponse
                                {
                                    Content = contentMediaTypes.Split(',').Select(x => new
                                    {
                                        Key = x.Trim(),
                                        value = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchemaReference("myobject"),
                                        }
                                    }).ToDictionary(x => x.Key, x => x.value)
                                },
                            }
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, IOpenApiSchema> {
                    {
                        "myobject", new OpenApiSchema {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema> {
                                {
                                    "id", new OpenApiSchema {
                                        Type = JsonSchemaType.String,
                                    }
                                }
                            },
                        }
                    }
                }
            }
        };
        document.RegisterComponents();
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(
            mockLogger.Object,
            new GenerationConfiguration
            {
                ClientClassName = "TestClient",
                ClientNamespaceName = "TestSdk",
                ApiRootUrl = "https://localhost",
                StructuredMimeTypes = new(structuredMimeTypes.Split(',').Select(x => x.Trim()))
            }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        CodeClass? rbClass = rbNS.Classes.FirstOrDefault(static x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        CodeMethod? generator = rbClass.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.RequestGenerator));
        Assert.NotNull(generator);
        foreach (string header in expectedAcceptHeader.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            Assert.Contains(header.Trim(), generator.AcceptedResponseTypes);
        }

        foreach (string header in unexpectedMimeTypes.Split(','))
        {
            Assert.DoesNotContain(header.Trim(), generator.AcceptedResponseTypes);
        }
    }
    [Theory]
    [InlineData("application/json, text/plain", "application/json", "application/json", "text/plain")]
    [InlineData("application/json, text/plain, application/yaml", "application/json;q=0.8,application/yaml", "application/yaml", "text/plain")]
    [InlineData("*/*", "application/json;q=0.8", "", "application/json")]
    [InlineData("application/json, */*", "application/json;q=0.8", "application/json", "*/*")]
    [InlineData("application/png, application/jpg", "application/json;q=0.8", "", "application/json")]
    public void GeneratesTheRightContentTypeHeaderBasedOnContentAndStatus(string contentMediaTypes, string structuredMimeTypes, string expectedContentTypeHeader, string unexpectedMimeTypes)
    {
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["answer"] = new OpenApiPathItem
                {
                    Operations = new()
                    {
                        [NetHttpMethod.Post] = new OpenApiOperation
                        {
                            RequestBody = new OpenApiRequestBody
                            {
                                Content = contentMediaTypes.Split(',').Select(x => new
                                {
                                    Key = x.Trim(),
                                    value = new OpenApiMediaType
                                    {
                                        Schema = new OpenApiSchemaReference("myobject"),
                                    }
                                }).ToDictionary(x => x.Key, x => x.value)
                            },
                        }
                    }
                }
            },
            Components = new()
            {
                Schemas = new Dictionary<string, IOpenApiSchema> {
                    {
                        "myobject", new OpenApiSchema {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema> {
                                {
                                    "id", new OpenApiSchema {
                                        Type = JsonSchemaType.String,
                                    }
                                }
                            },
                        }
                    }
                }
            }
        };
        document.RegisterComponents();
        document.SetReferenceHostDocument();
        Mock<ILogger<ReQuestyBuilder>> mockLogger = new();
        ReQuestyBuilder builder = new(
            mockLogger.Object,
            new GenerationConfiguration
            {
                ClientClassName = "TestClient",
                ClientNamespaceName = "TestSdk",
                ApiRootUrl = "https://localhost",
                StructuredMimeTypes = new(structuredMimeTypes.Split(',').Select(x => x.Trim()))
            }, _httpClient);
        OpenApiUrlTreeNode node = builder.CreateUriSpace(document);
        CodeNamespace codeModel = builder.CreateSourceModel(node);
        CodeNamespace? rbNS = codeModel.FindNamespaceByName("TestSdk.Answer");
        Assert.NotNull(rbNS);
        CodeClass? rbClass = rbNS.Classes.FirstOrDefault(static x => x.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.NotNull(rbClass);
        CodeMethod? generator = rbClass.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.RequestGenerator));
        Assert.NotNull(generator);
        if (string.IsNullOrEmpty(expectedContentTypeHeader))
        {
            Assert.Empty(generator.RequestBodyContentType);
            Assert.NotNull(generator.Parameters.OfKind(CodeParameterKind.RequestBodyContentType));
        }
        else
        {
            foreach (string header in expectedContentTypeHeader.Split(','))
            {
                Assert.Contains(header.Trim(), generator.RequestBodyContentType);
            }
        }

        foreach (string header in unexpectedMimeTypes.Split(','))
        {
            Assert.DoesNotContain(header.Trim(), generator.RequestBodyContentType);
        }
    }
}
