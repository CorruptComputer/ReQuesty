using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Extensions;

namespace ReQuesty.Builder.PathSegmenters;
public class HttpPathSegmenter(string rootPath, string clientNamespaceName) : CommonPathSegmenter(rootPath, clientNamespaceName)
{
    public override string FileSuffix => ".http";
    public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterUpperCase();
    public override string NormalizeFileName(CodeElement currentElement)
    {
        return GetLastFileNameSegment(currentElement).ToFirstCharacterUpperCase();
    }
}
