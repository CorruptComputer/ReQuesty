using ReQuesty.Builder.CodeDOM;

namespace ReQuesty.Builder.Writers.CSharp;
public class CodeBlockEndWriter(CSharpConventionService conventionService) : BaseElementWriter<BlockEnd, CSharpConventionService>(conventionService)
{
    public override void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.CloseBlock();
        if (codeElement?.Parent is CodeClass codeClass && codeClass.Parent is CodeNamespace)
        {
            writer.CloseBlock();
            CSharpConventionService.WriteNullableMiddle(writer);
            CSharpConventionService.WritePragmaRestore(writer, CSharpConventionService.CS0618);
        }
    }
}
