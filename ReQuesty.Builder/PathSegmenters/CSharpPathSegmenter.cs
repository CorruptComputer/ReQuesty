using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Extensions;

namespace ReQuesty.Builder.PathSegmenters;
public class CSharpPathSegmenter(string rootPath, string clientNamespaceName) : CommonPathSegmenter(rootPath, clientNamespaceName)
{
    public override string FileSuffix => ".cs";
    public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterUpperCase().ShortenFileName();
    public override string NormalizeFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToFirstCharacterUpperCase().ShortenFileName();
    public override string NormalizePath(string fullPath)
    {
        if (ExceedsMaxPathLength(fullPath) && Path.GetDirectoryName(fullPath) is string directoryName)
        {
            int availableLength = MaxFilePathLength - (directoryName.Length + FileSuffix.Length + 2); // one for the folder separator and another to ensure its below limit
            return Path.Combine(directoryName, Path.GetFileName(fullPath).ShortenFileName(availableLength)[..Math.Min(64, availableLength)]) + FileSuffix;
        }
        return fullPath;
    }
    internal const int MaxFilePathLength = 32767;
    private static bool ExceedsMaxPathLength(string fullPath) => !string.IsNullOrEmpty(fullPath) && fullPath.Length > MaxFilePathLength;
}
