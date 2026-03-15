using System.Text.Json.Nodes;
using ReQuesty.Builder.OpenApiExtensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace ReQuesty.Builder.Extensions;

public static class OpenApiSettingsExtensions
{
    public static void AddGenerationExtensions(this OpenApiReaderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.AddMicrosoftExtensionParsers();
        settings.ExtensionParsers ??= new Dictionary<string, Func<JsonNode, OpenApiSpecVersion, IOpenApiExtension>>(StringComparer.OrdinalIgnoreCase);
        settings.ExtensionParsers.TryAdd(OpenApiReQuestyExtension.Name, static (i, _) => OpenApiReQuestyExtension.Parse(i));
    }
}
