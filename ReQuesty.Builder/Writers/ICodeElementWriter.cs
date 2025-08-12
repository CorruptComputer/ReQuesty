using ReQuesty.Builder.CodeDOM;

namespace ReQuesty.Builder.Writers;
public interface ICodeElementWriter<T> where T : CodeElement
{
    void WriteCodeElement(T codeElement, LanguageWriter writer);
}
