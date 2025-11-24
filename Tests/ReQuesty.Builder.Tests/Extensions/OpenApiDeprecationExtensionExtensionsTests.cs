using ReQuesty.Builder.Extensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.MicrosoftExtensions;
using Xunit;
using NetHttpMethod = System.Net.Http.HttpMethod;

namespace ReQuesty.Builder.Tests.Extensions;

public class OpenApiDeprecationExtensionExtensions
{
    [Fact]
    public void ToDeprecationInformation()
    {
        OpenApiDeprecationExtension openApiExtension = new()
        {
            Description = "description",
            Version = "version",
            RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
            Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = openApiExtension.ToDeprecationInformation();
        Assert.Equal(openApiExtension.Description, deprecationInformation.DescriptionTemplate);
        Assert.Equal(openApiExtension.Version, deprecationInformation.Version);
        Assert.Equal(openApiExtension.RemovalDate.Value.Year, deprecationInformation.RemovalDate!.Value.Year);
        Assert.Equal(openApiExtension.Date.Value.Month, deprecationInformation.Date!.Value.Month);
    }
    [Fact]
    public void GetsDeprecationInformationFromOpenApiSchema()
    {
        OpenApiSchema openApiSchema = new()
        {
            Deprecated = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = openApiSchema.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsEmptyDeprecationInformationFromSchema()
    {
        OpenApiSchema openApiSchema = new()
        {
            Deprecated = true,
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = openApiSchema.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationInformationFromNonDeprecatedSchema()
    {
        OpenApiSchema openApiSchema = new()
        {
            Deprecated = false,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = openApiSchema.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationOnOperationDirect()
    {
        OpenApiOperation operation = new()
        {
            Deprecated = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationOnNonDeprecatedOperation()
    {
        OpenApiOperation operation = new()
        {
            Deprecated = false,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationOnOperationWithNullResponseContentTypeInstance()
    {
        OpenApiOperation operation = new()
        {
            Deprecated = false,
            Responses = new OpenApiResponses
            {
                {
                    "200", new OpenApiResponse
                    {
                        Content = new Dictionary<string, IOpenApiMediaType>()
                        {
                            { "application/json", null!
                            }
                        }
                    }
                }
            }
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationOnOperationWithDeprecatedInlineResponseSchema()
    {
        OpenApiOperation operation = new()
        {
            Deprecated = false,
            Responses = new OpenApiResponses
            {
                {
                    "200", new OpenApiResponse
                    {
                        Content = new Dictionary<string, IOpenApiMediaType>()
                        {
                            { "application/json", new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Deprecated = true,
                                        Extensions = new Dictionary<string, IOpenApiExtension>
                                        {
                                            { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                                                Description = "description",
                                                Version = "version",
                                                RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                                                Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                                            } }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationOnOperationWithDeprecatedReferenceResponseSchema()
    {
        OpenApiSchema schema = new()
        {
            Deprecated = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        OpenApiDocument document = new();
        document.AddComponent("schema", schema);
        OpenApiOperation operation = new()
        {
            Deprecated = false,
            Responses = new OpenApiResponses
            {
                {
                    "200", new OpenApiResponse
                    {
                        Content = new Dictionary<string, IOpenApiMediaType>()
                        {
                            { "application/json", new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchemaReference("schema", document)
                                }
                            }
                        }
                    }
                }
            }
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationOnOperationWithDeprecatedInlineRequestSchema()
    {
        OpenApiOperation operation = new()
        {
            Deprecated = false,
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, IOpenApiMediaType>()
                {
                    { "application/json", new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Deprecated = true,
                                Extensions = new Dictionary<string, IOpenApiExtension>
                                {
                                    { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                                        Description = "description",
                                        Version = "version",
                                        RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                                        Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                                    } }
                                }
                            }
                        }
                    }
                }
            }
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationOnOperationWithNullRequestBodyContentTypeInstance()
    {
        OpenApiOperation operation = new()
        {
            Deprecated = false,
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, IOpenApiMediaType>()
                {
                    { "application/json", null!
                    }
                }
            }
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationOnOperationWithDeprecatedReferenceRequestSchema()
    {
        OpenApiSchema schema = new()
        {
            Deprecated = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        OpenApiDocument document = new();
        document.AddComponent("schema", schema);
        OpenApiOperation operation = new()
        {
            Deprecated = false,
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, IOpenApiMediaType>()
                {
                    { "application/json", new OpenApiMediaType
                        {
                            Schema = new OpenApiSchemaReference("schema", document)
                        }
                    }
                }
            }
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = operation.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationInformationOnParameter()
    {
        OpenApiParameter parameter = new()
        {
            Deprecated = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = parameter.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationInformationOnNonDeprecatedParameter()
    {
        OpenApiParameter parameter = new()
        {
            Deprecated = false,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = parameter.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationInformationOnParameterWithDeprecatedInlineSchema()
    {
        OpenApiParameter parameter = new()
        {
            Deprecated = false,
            Schema = new OpenApiSchema
            {
                Deprecated = true,
                Extensions = new Dictionary<string, IOpenApiExtension>
                {
                    { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                        Description = "description",
                        Version = "version",
                        RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                        Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    } }
                }
            }
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = parameter.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationInformationOnParameterWithDeprecatedReferenceSchema()
    {
        OpenApiSchema schema = new()
        {
            Deprecated = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        OpenApiDocument document = new();
        document.AddComponent("schema", schema);
        OpenApiParameter parameter = new()
        {
            Deprecated = false,
            Schema = new OpenApiSchemaReference("schema", document)
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = parameter.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationInformationOnParameterWithDeprecatedInlineContentSchema()
    {
        OpenApiParameter parameter = new()
        {
            Deprecated = false,
            Content = new Dictionary<string, IOpenApiMediaType>() {
                { "application/json", new OpenApiMediaType()
                    {
                        Schema = new OpenApiSchema
                        {
                            Deprecated = true,
                            Extensions = new Dictionary<string, IOpenApiExtension>
                            {
                                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                                    Description = "description",
                                    Version = "version",
                                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                                } }
                            }
                        }
                    }
                }
            }
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = parameter.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationInformationOnParameterWithDeprecatedReferenceContentSchema()
    {
        OpenApiSchema schema = new()
        {
            Deprecated = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                    Description = "description",
                    Version = "version",
                    RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                    Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                } }
            }
        };
        OpenApiDocument document = new();
        document.AddComponent("schema", schema);
        OpenApiParameter parameter = new()
        {
            Deprecated = false,
            Content = new Dictionary<string, IOpenApiMediaType>() {
                { "application/json", new OpenApiMediaType()
                    {
                        Schema = new OpenApiSchemaReference("schema", document)
                    }
                }
            }
        };
        Builder.CodeDOM.DeprecationInformation deprecationInformation = parameter.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsDeprecationInformationFromTreeNodeWhenAllOperationsDeprecated()
    {
        OpenApiUrlTreeNode rootNode = OpenApiUrlTreeNode.Create();
        OpenApiUrlTreeNode treeNode = rootNode.Attach("foo", new OpenApiPathItem()
        {
            Operations = new Dictionary<NetHttpMethod, OpenApiOperation>()
            {
                {NetHttpMethod.Get, new OpenApiOperation{
                    Deprecated = true,
                    Extensions = new Dictionary<string, IOpenApiExtension>
                    {
                        { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                            Description = "description",
                            Version = "version",
                            RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                            Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                        } }
                    }
                } },
            }
        }, Constants.DefaultOpenApiLabel);
        Builder.CodeDOM.DeprecationInformation deprecationInformation = treeNode.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.True(deprecationInformation.IsDeprecated);
        Assert.Equal("description", deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationInformationFromTreeNodeOnNoOperation()
    {
        OpenApiUrlTreeNode rootNode = OpenApiUrlTreeNode.Create();
        OpenApiUrlTreeNode treeNode = rootNode.Attach("foo", new OpenApiPathItem()
        {
            Operations = []
        }, Constants.DefaultOpenApiLabel);
        Builder.CodeDOM.DeprecationInformation deprecationInformation = treeNode.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
    [Fact]
    public void GetsNoDeprecationInformationFromTreeNodeWhenOneOperationNonDeprecated()
    {
        OpenApiUrlTreeNode rootNode = OpenApiUrlTreeNode.Create();
        OpenApiUrlTreeNode treeNode = rootNode.Attach("foo", new OpenApiPathItem()
        {
            Operations = new Dictionary<NetHttpMethod, OpenApiOperation>()
            {
                {NetHttpMethod.Get, new OpenApiOperation{
                    Deprecated = true,
                    Extensions = new Dictionary<string, IOpenApiExtension>
                    {
                        { OpenApiDeprecationExtension.Name, new OpenApiDeprecationExtension {
                            Description = "description",
                            Version = "version",
                            RemovalDate = new DateTimeOffset(2023, 05, 04, 0, 0, 0, TimeSpan.Zero),
                            Date = new DateTimeOffset(2021, 05, 04, 0, 0, 0, TimeSpan.Zero),
                        } }
                    }
                } },
                {NetHttpMethod.Post, new OpenApiOperation{
                    Deprecated = false,
                }}
            }
        }, Constants.DefaultOpenApiLabel);
        Builder.CodeDOM.DeprecationInformation deprecationInformation = treeNode.GetDeprecationInformation();
        Assert.NotNull(deprecationInformation);
        Assert.False(deprecationInformation.IsDeprecated);
        Assert.Null(deprecationInformation.DescriptionTemplate);
    }
}
