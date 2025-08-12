using ReQuesty.Builder.CodeDOM;

namespace ReQuesty.Builder.Writers;

internal class CodePropertyTypeComparer(bool orderByDesc = false) : IComparer<CodeProperty>
{
    private readonly CodeTypeComparer TypeComparer = new(orderByDesc);

    public int Compare(CodeProperty? x, CodeProperty? y)
    {
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ => TypeComparer.Compare(x?.Type, y?.Type),
        };
    }
}
