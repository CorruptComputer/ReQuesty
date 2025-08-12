using ReQuesty.Builder.PathSegmenters;

namespace ReQuesty.Builder.Writers.CSharp;
public class CSharpWriter : LanguageWriter
{
    public CSharpWriter(string rootPath, string clientNamespaceName)
    {
        PathSegmenter = new CSharpPathSegmenter(rootPath, clientNamespaceName);
        CSharpConventionService conventionService = new();
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeBlockEndWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeIndexerWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeTypeWriter(conventionService));

    }
}
