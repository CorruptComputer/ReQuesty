using ReQuesty.Builder.CodeDOM;

using Xunit;

namespace ReQuesty.Builder.Tests.CodeDOM;
public class CodeEnumTests
{
    [Fact]
    public void EnumInits()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeEnum codeEnum = root.AddEnum(new CodeEnum
        {
            Name = "Enum",
            Documentation = new()
            {
                DescriptionTemplate = "some description",
            },
            Flags = true,
        }).First();
        codeEnum.AddOption(new CodeEnumOption { Name = "option1" });
    }
}
