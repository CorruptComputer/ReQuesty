using System.Diagnostics.CodeAnalysis;

using ReQuesty.Builder.CodeDOM;

namespace ReQuesty.Builder.Refiners;
public class CodeUsingDeclarationNameComparer(StringComparer? stringComparer = null) : IEqualityComparer<CodeUsing>
{
    private readonly StringComparer _stringComparer = stringComparer ?? StringComparer.OrdinalIgnoreCase;
    public bool Equals(CodeUsing? x, CodeUsing? y)
    {
        if (x is null || y is null)
        {
            return object.Equals(x, y);
        }

        return _stringComparer.Equals(x.Name, y.Name)
               && _stringComparer.Equals(x.Declaration?.Name, y.Declaration?.Name);
    }

    public int GetHashCode([DisallowNull] CodeUsing obj)
    {
        HashCode hash = new();
        if (obj is null)
        {
            return hash.ToHashCode();
        }

        hash.Add(obj.Name, _stringComparer);
        hash.Add(obj.Declaration?.Name, _stringComparer);
        return hash.ToHashCode();
    }
}
