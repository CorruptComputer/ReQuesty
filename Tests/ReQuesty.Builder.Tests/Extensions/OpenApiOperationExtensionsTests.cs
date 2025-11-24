using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Extensions;

using Microsoft.OpenApi;

using Xunit;

namespace ReQuesty.Builder.Tests.Extensions;

public class OpenApiOperationExtensionsTests
{
    [Fact]
    public void GetsResponseSchema()
    {
        OpenApiOperation operation = new()
        {
            Responses = new() {
                { "200", new OpenApiResponse() {
                    Content = new Dictionary<string, IOpenApiMediaType> {
                        {"application/json", new OpenApiMediaType() {
                            Schema = new OpenApiSchema()
                        }}
                    }
                }}
            }
        };
        OpenApiOperation operation2 = new()
        {
            Responses = new() {
                { "400", new OpenApiResponse() {
                    Content = new Dictionary<string, IOpenApiMediaType> {
                        {"application/json", new OpenApiMediaType() {
                            Schema = new OpenApiSchema()
                        }}
                    }
                }}
            }
        };
        OpenApiOperation operation3 = new()
        {
            Responses = new() {
                { "200", new OpenApiResponse() {
                    Content = new Dictionary<string, IOpenApiMediaType> {
                        {"application/invalid", new OpenApiMediaType() {
                            Schema = new OpenApiSchema()
                        }}
                    }
                }}
            }
        };
        GenerationConfiguration defaultConfiguration = new();
        Assert.NotNull(operation.GetResponseSchema(defaultConfiguration.StructuredMimeTypes));
        Assert.Null(operation2.GetResponseSchema(defaultConfiguration.StructuredMimeTypes));
        Assert.Null(operation3.GetResponseSchema(defaultConfiguration.StructuredMimeTypes));
    }
    [Fact]
    public void Defensive()
    {
        Dictionary<string, IOpenApiMediaType> source = [];
        Assert.Empty(source.GetValidSchemas(["application/json"]));
        Assert.Throws<ArgumentNullException>(() => source.GetValidSchemas(null!));
    }
}
