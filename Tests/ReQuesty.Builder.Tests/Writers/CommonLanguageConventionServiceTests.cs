using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Writers.CSharp;

using Moq;

using Xunit;

namespace ReQuesty.Builder.Tests.Writers;
public class CommonLanguageConventionServiceTests
{
    [Fact]
    public void TranslatesType()
    {
        CSharpConventionService service = new();
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        Mock<CodeTypeBase> unknownTypeMock = new();
        unknownTypeMock.Setup(x => x.Name).Returns("unknownType");
        Assert.Throws<InvalidOperationException>(() => service.TranslateType(unknownTypeMock.Object));
        CodeType stringType = new()
        {
            Name = "string"
        };
        Assert.Equal("string", service.TranslateType(stringType));
        CodeUnionType unionStringType = new()
        {
            Name = "unionString"
        };
        unionStringType.AddType(stringType);
        Assert.Equal("string", service.TranslateType(unionStringType));
    }
}
