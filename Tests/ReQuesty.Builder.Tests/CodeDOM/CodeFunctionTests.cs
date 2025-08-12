using ReQuesty.Builder.CodeDOM;

using Xunit;

namespace ReQuesty.Builder.Tests.CodeDOM;

public class CodeFunctionTests
{
    [Fact]
    public void Defensive()
    {
        CodeMethod method = new()
        {
            Name = "class",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        Assert.Throws<ArgumentNullException>(() => new CodeFunction(null!));
        Assert.Throws<InvalidOperationException>(() => new CodeFunction(method));
        method.IsStatic = true;
        Assert.Throws<InvalidOperationException>(() => new CodeFunction(method));
        CodeClass parentClass = new();
        method.Parent = parentClass;
        CodeFunction function = new(method);
        Assert.Equal(method, function.OriginalLocalMethod);
        Assert.Equal(parentClass, function.OriginalMethodParentClass);
    }
}
