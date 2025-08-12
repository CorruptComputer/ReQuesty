using System.Text.Json;
using System.Text.Json.Nodes;
using ReQuesty.Builder.Extensions;
using Microsoft.OpenApi;

namespace ReQuesty.Builder.OpenApiExtensions;

public class OpenApiLogoExtension : IOpenApiExtension
{
    public static string Name => "x-logo";
#pragma warning disable CA1056
    public string? Url
#pragma warning restore CA1056
    {
        get; set;
    }
    public static OpenApiLogoExtension Parse(JsonNode source)
    {
        if (source is not JsonObject rawObject)
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        OpenApiLogoExtension extension = new();
        if (rawObject.TryGetPropertyValue(nameof(Url).ToFirstCharacterLowerCase(), out JsonNode? url) && url is JsonValue urlValue && urlValue.GetValueKind() is JsonValueKind.String && urlValue.TryGetValue<string>(out string? urlStrValue))
        {
            extension.Url = urlStrValue;
        }
        return extension;
    }

    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        if (!string.IsNullOrEmpty(Url))
        {
            writer.WritePropertyName(nameof(Url).ToFirstCharacterLowerCase());
            writer.WriteValue(Url);
        }
        writer.WriteEndObject();
    }
}
