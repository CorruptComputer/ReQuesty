using ReQuesty.Builder.CodeDOM;

using Xunit;

namespace ReQuesty.Builder.Tests.CodeDOM;
public class CodeUnionTypeTests
{
    [Fact]
    public void ClonesProperly()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeUnionType type = new()
        {
            Name = "type1",
        };
        type.AddType(new CodeType
        {
            Name = "subtype"
        });
        CodeUnionType? clone = type.Clone() as CodeUnionType;
        Assert.NotNull(clone);
        Assert.Single(clone.AllTypes);
        Assert.Single(clone.Types);
        Assert.Equal(type.AllTypes.First().Name, clone.AllTypes.First().Name);
    }
}
