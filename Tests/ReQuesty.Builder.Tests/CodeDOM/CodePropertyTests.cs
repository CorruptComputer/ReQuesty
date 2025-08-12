using ReQuesty.Builder.CodeDOM;

using Xunit;

namespace ReQuesty.Builder.Tests.CodeDOM;
public class CodePropertyTests
{
    [Fact]
    public void Defensive()
    {
        CodeProperty property = new()
        {
            Name = "prop",
            Type = new CodeType
            {
                Name = "string",
            },
        };
        Assert.False(property.IsOfKind(null!));
        Assert.False(property.IsOfKind([]));
    }
    [Fact]
    public void IsOfKind()
    {
        CodeProperty property = new()
        {
            Name = "prop",
            Type = new CodeType
            {
                Name = "string",
            },
        };
        Assert.False(property.IsOfKind(CodePropertyKind.BackingStore));
        property.Kind = CodePropertyKind.RequestBuilder;
        Assert.True(property.IsOfKind(CodePropertyKind.RequestBuilder));
        Assert.True(property.IsOfKind(CodePropertyKind.RequestBuilder, CodePropertyKind.BackingStore));
        Assert.False(property.IsOfKind(CodePropertyKind.BackingStore));
    }
}
