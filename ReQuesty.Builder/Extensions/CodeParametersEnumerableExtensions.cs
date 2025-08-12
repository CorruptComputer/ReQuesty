using ReQuesty.Builder.CodeDOM;

namespace ReQuesty.Builder.Extensions;
public static class CodeParametersEnumerableExtensions
{
    public static CodeParameter? OfKind(this IEnumerable<CodeParameter> parameters, CodeParameterKind kind)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        return parameters.FirstOrDefault(x => x != null && x.IsOfKind(kind));
    }
}
