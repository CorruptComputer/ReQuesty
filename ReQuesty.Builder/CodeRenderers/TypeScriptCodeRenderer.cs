using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Configuration;

namespace ReQuesty.Builder.CodeRenderers;
public class TypeScriptCodeRenderer(GenerationConfiguration configuration) : CodeRenderer(configuration)
{
    private CodeNamespace? modelsNamespace;

    public override bool ShouldRenderNamespaceFile(CodeNamespace codeNamespace)
    {
        if (codeNamespace is null)
        {
            return false;
        }

        modelsNamespace ??= codeNamespace.GetRootNamespace().FindChildByName<CodeNamespace>(Configuration.ModelsNamespaceName);
        if (modelsNamespace is not null && !modelsNamespace.IsParentOf(codeNamespace) && modelsNamespace != codeNamespace)
        {
            return false;
        }

        return codeNamespace.Interfaces.Any() || codeNamespace.Files.Any(static x => x.Interfaces.Any());
    }
}
