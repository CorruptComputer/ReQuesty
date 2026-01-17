using ReQuesty.Builder.CodeDOM;

namespace ReQuesty.Builder.Refiners;

public record AdditionalUsingEvaluator(Func<CodeElement, bool> CodeElementEvaluator, string NamespaceName, bool IsErasable = false, params string[] ImportSymbols)
{
    public AdditionalUsingEvaluator(Func<CodeElement, bool> CodeElementEvaluator, string NamespaceName, params string[] ImportSymbols) : this(CodeElementEvaluator, NamespaceName, false, ImportSymbols) { }
}
