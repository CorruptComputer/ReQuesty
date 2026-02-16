using ReQuesty.Builder.CodeDOM;

namespace ReQuesty.Builder.Extensions;

internal static class CodePropertiesEnumerableExtensions
{
    public static CodeProperty? FirstOrDefaultOfKind(this IEnumerable<CodeProperty> properties, params CodePropertyKind[] kinds)
    {
        ArgumentNullException.ThrowIfNull(properties);
        return properties.FirstOrDefault(x => x is not null && x.IsOfKind(kinds));
    }
    public static IEnumerable<CodeProperty> OfKind(this IEnumerable<CodeProperty> properties, params CodePropertyKind[] kinds)
    {
        ArgumentNullException.ThrowIfNull(properties);
        return properties.Where(x => x is not null && x.IsOfKind(kinds));
    }
}
