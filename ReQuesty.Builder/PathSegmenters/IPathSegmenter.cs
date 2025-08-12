using ReQuesty.Builder.CodeDOM;

namespace ReQuesty.Builder.PathSegmenters;
public interface IPathSegmenter
{
    string GetPath(CodeNamespace currentNamespace, CodeElement currentElement, bool shouldNormalizePath = true);
}
