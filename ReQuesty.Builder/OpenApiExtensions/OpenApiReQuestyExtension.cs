using System.Text.Json.Nodes;
using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Extensions;
using Microsoft.OpenApi;

namespace ReQuesty.Builder.OpenApiExtensions;

public class OpenApiReQuestyExtension : IOpenApiExtension
{
    /// <summary>
    /// Name of the extension as used in the description.
    /// </summary>
    public static string Name => "x-requesty-info";

#pragma warning disable CA2227
    public LanguagesInformation LanguagesInformation { get; set; } = [];
#pragma warning restore CA2227

    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (LanguagesInformation != null &&
            LanguagesInformation.Any())
        {
            writer.WriteStartObject();
            writer.WriteRequiredObject(nameof(LanguagesInformation).ToFirstCharacterLowerCase(), LanguagesInformation, (w, x) => x.SerializeAsV3(w));
            writer.WriteEndObject();
        }
    }
    public static OpenApiReQuestyExtension Parse(JsonNode source)
    {
        if (source is not JsonObject jsonNode)
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        OpenApiReQuestyExtension extension = new();
        if (jsonNode.TryGetPropertyValue(nameof(LanguagesInformation).ToFirstCharacterLowerCase(), out JsonNode? languagesInfo) && languagesInfo is JsonObject objectValue)
        {
            extension.LanguagesInformation = LanguagesInformation.Parse(objectValue);
        }
        return extension;
    }
}
