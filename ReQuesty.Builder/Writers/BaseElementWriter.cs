using ReQuesty.Builder.CodeDOM;

namespace ReQuesty.Builder.Writers;
public abstract class BaseElementWriter<TElement, TConventionsService>(TConventionsService conventionService) : ICodeElementWriter<TElement> where TElement : CodeElement where TConventionsService : ILanguageConventionService
{
    protected TConventionsService conventions
    {
        get; init;
    } = conventionService ?? throw new ArgumentNullException(nameof(conventionService));

    public abstract void WriteCodeElement(TElement codeElement, LanguageWriter writer);
}
