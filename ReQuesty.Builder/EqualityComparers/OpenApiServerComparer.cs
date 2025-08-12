using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi;

namespace ReQuesty.Builder.EqualityComparers;

internal sealed class OpenApiServerComparer : IEqualityComparer<OpenApiServer>
{
    private static ReadOnlySpan<char> TrimStartInsensitive(ReadOnlySpan<char> span, ReadOnlySpan<char> toTrim)
    {
        if (span.Length < toTrim.Length)
        {
            return span;
        }

        bool matched = true;
        for (int i = 0; i < toTrim.Length; i++)
        {
            if (Char.ToLowerInvariant(span[i]) == Char.ToLowerInvariant(toTrim[i]))
            {
                continue;
            }

            matched = false;
            break;
        }

        return matched ? span[toTrim.Length..] : span;
    }
    private static ReadOnlySpan<char> TrimProtocol(ReadOnlySpan<char> output)
    {
        output = TrimStartInsensitive(output, "http://");
        output = TrimStartInsensitive(output, "https://");
        return output;
    }
    public bool Equals(OpenApiServer? x, OpenApiServer? y)
    {
        if (x?.Url is null || y?.Url is null)
        {
            return object.Equals(x, y);
        }

        ReadOnlySpan<char> x0 = TrimProtocol(x.Url);
        ReadOnlySpan<char> y0 = TrimProtocol(y.Url);
        return x0.Equals(y0, StringComparison.OrdinalIgnoreCase);
    }
    public int GetHashCode([DisallowNull] OpenApiServer obj)
    {
        HashCode hash = new();
        if (string.IsNullOrEmpty(obj?.Url))
        {
            return hash.ToHashCode();
        }

        ReadOnlySpan<char> url = TrimProtocol(obj.Url);
        // hash can't compute ReadOnlySpan<char>. ReadOnlySpan also doesn't have a working GetHashCode method.
        foreach (char c in url)
        {
            hash.Add(Char.ToLowerInvariant(c));
        }

        return hash.ToHashCode();
    }
}
