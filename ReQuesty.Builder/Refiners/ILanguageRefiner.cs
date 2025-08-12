using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Configuration;

namespace ReQuesty.Builder.Refiners;

public interface ILanguageRefiner
{
    Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken);
    public static async Task RefineAsync(GenerationConfiguration config, CodeNamespace generatedCode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        switch (config.Language)
        {
            case GenerationLanguage.CSharp:
                await new CSharpRefiner(config).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
        }
    }
}
