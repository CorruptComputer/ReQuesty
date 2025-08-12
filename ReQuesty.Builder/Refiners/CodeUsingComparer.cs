using System.Diagnostics.CodeAnalysis;

using ReQuesty.Builder.CodeDOM;

namespace ReQuesty.Builder.Refiners;
public class CodeUsingComparer(bool compareOnDeclaration, StringComparer? stringComparer = null) : IEqualityComparer<CodeUsing>
{
    private readonly StringComparer _stringComparer = stringComparer ?? StringComparer.Ordinal;

    public bool Equals(CodeUsing? x, CodeUsing? y)
    {
        return (!compareOnDeclaration || x?.Declaration == y?.Declaration) && _stringComparer.Equals(x?.Name, y?.Name);
    }

    public int GetHashCode([DisallowNull] CodeUsing obj)
    {
        HashCode hash = new();
        if (compareOnDeclaration)
        {
            hash.Add(obj?.Declaration);
        }

        hash.Add(obj?.Name, _stringComparer);
        return hash.ToHashCode();
    }
}
