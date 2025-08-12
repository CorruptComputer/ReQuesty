using ReQuesty.Builder.Validation;
using Microsoft.OpenApi;
using Xunit;

namespace ReQuesty.Builder.Tests.Validation;

public class OpenApiSchemaComparerTests
{
    private readonly OpenApiSchemaComparer _comparer = new();
    [Fact]
    public void Defensive()
    {
        Assert.Equal(new HashCode().ToHashCode(), _comparer.GetHashCode(null!));
        Assert.True(_comparer.Equals(null, null));
        Assert.False(_comparer.Equals(new OpenApiSchema(), null));
        Assert.False(_comparer.Equals(null, new OpenApiSchema()));
    }

    [Fact]
    public void TestEquals()
    {
        Assert.True(_comparer.Equals(new OpenApiSchema(), new OpenApiSchema()));
    }
    [Fact]
    public void DoesNotStackOverFlowOnCircularReferencesForEquals()
    {
        OpenApiSchema schema = new()
        {
            AnyOf = [],
            Properties = new Dictionary<string, IOpenApiSchema>(),
        };
        schema.Properties.Add("test", schema);
        schema.AnyOf.Add(schema);
        OpenApiSchema schema2 = new()
        {
            AnyOf = [],
            Properties = new Dictionary<string, IOpenApiSchema>(),
        };
        schema2.Properties.Add("test", schema2);
        schema2.AnyOf.Add(schema2);
        Assert.True(_comparer.Equals(schema, schema2));
    }
}
