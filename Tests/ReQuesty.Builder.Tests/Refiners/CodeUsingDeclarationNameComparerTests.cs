using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Refiners;

using Xunit;

namespace ReQuesty.Builder.Tests.Refiners;
public class CodeUsingDeclarationNameComparerTests
{
    private readonly CodeUsingDeclarationNameComparer comparer = new();
    [Fact]
    public void Defensive()
    {
        Assert.True(comparer.Equals(null, null));
        Assert.False(comparer.Equals(new(), null));
        Assert.False(comparer.Equals(null, new()));
        Assert.True(comparer.Equals(new(), new()));
        HashCode hash = new();
        Assert.Equal(hash.ToHashCode(), comparer.GetHashCode(null!));
        hash = new HashCode();
        hash.Add(string.Empty);
        hash.Add<string>(null!);
        Assert.Equal(hash.ToHashCode(), comparer.GetHashCode(new()));
    }
    [Fact]
    public void SameImportsReturnSameHashCode()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace graphNS = root.AddNamespace("Graph");
        CodeNamespace modelsNS = root.AddNamespace($"{graphNS.Name}.Models");
        CodeNamespace rbNS = root.AddNamespace($"{graphNS.Name}.Me");
        CodeClass modelClass = modelsNS.AddClass(new CodeClass { Name = "Model" }).First();
        CodeClass rbClass = rbNS.AddClass(new CodeClass { Name = "UserRequestBuilder" }).First();
        CodeUsing using1 = new()
        {
            Name = modelsNS.Name,
            Declaration = new CodeType
            {
                Name = modelClass.Name,
                TypeDefinition = modelClass,
            }
        };
        CodeUsing using2 = new()
        {
            Name = modelsNS.Name.ToUpperInvariant(),
            Declaration = new CodeType
            {
                Name = modelClass.Name.ToLowerInvariant(),
                TypeDefinition = modelClass,
            }
        };
        rbClass.AddUsing(using1);
        rbClass.AddUsing(using2);
        Assert.Equal(comparer.GetHashCode(using1), comparer.GetHashCode(using2));
    }
}
