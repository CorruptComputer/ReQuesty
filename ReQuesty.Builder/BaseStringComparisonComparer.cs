namespace ReQuesty.Builder;

public abstract class BaseStringComparisonComparer<T> : IComparer<T>
{
    public abstract int Compare(T? x, T? y);
    public int CompareStrings(string? x, string? y, StringComparer comparer)
    {
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ => NormalizeComparisonResult(comparer.Compare(x, y)),
        };
    }
    private static int NormalizeComparisonResult(int result)
    {
        if (result < 0)
        {
            return -1;
        }
        else if (result > 0)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}
