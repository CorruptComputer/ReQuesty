using ReQuesty.Builder.CodeDOM;

using Xunit;

namespace ReQuesty.Builder.Tests.CodeDOM;
public class CodeParameterTests
{
    [Fact]
    public void Defensive()
    {
        CodeParameter parameter = new()
        {
            Name = "class",
            Type = new CodeType
            {
                Name = "string"
            }
        };
        Assert.False(parameter.IsOfKind(null!));
        Assert.False(parameter.IsOfKind([]));
    }
    [Fact]
    public void IsOfKind()
    {
        CodeParameter parameter = new()
        {
            Name = "class",
            Type = new CodeType
            {
                Name = "string"
            }
        };
        Assert.False(parameter.IsOfKind(CodeParameterKind.RequestConfiguration));
        parameter.Kind = CodeParameterKind.RequestAdapter;
        Assert.True(parameter.IsOfKind(CodeParameterKind.RequestAdapter));
        Assert.True(parameter.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.RequestConfiguration));
        Assert.False(parameter.IsOfKind(CodeParameterKind.RequestConfiguration));
    }
}
