using ReQuesty.Builder.CodeDOM;

namespace ReQuesty.Builder.Writers;
public class CodeTypeWriter(ILanguageConventionService conventionService) : BaseElementWriter<CodeType, ILanguageConventionService>(conventionService)
{
    public override void WriteCodeElement(CodeType codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(conventions.GetTypeString(codeElement, codeElement), includeIndent: false);
    }
}
