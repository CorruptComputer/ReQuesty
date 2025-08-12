using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Refiners;

using Xunit;

namespace ReQuesty.Builder.Tests.CodeDOM;
public class CodeUsingComparerTests
{
    [Fact]
    public void ComparesWithDeclaration()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeUsing cUsing = new()
        {
            Name = "using1",
        };
        cUsing.Declaration = new CodeType
        {
            Name = "type1"
        };

        CodeUsing cUsing2 = new()
        {
            Name = "using2",
        };
        cUsing2.Declaration = new CodeType
        {
            Name = "type2"
        };
        CodeUsingComparer comparer = new(true);
        Assert.False(comparer.Equals(cUsing, cUsing2));
        Assert.NotEqual(0, comparer.GetHashCode(cUsing));
    }
}
