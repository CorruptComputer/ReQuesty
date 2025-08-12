using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace ReQuesty.Builder.Configuration;

public class LanguagesInformation : Dictionary<string, LanguageInformation>, IOpenApiSerializable, ICloneable
{
    public void SerializeAsV2(IOpenApiWriter writer) => SerializeAsV31(writer);
    public void SerializeAsV3(IOpenApiWriter writer) => SerializeAsV31(writer);
    public static LanguagesInformation Parse(JsonObject jsonNode)
    {
        LanguagesInformation extension = [];
        foreach (KeyValuePair<string, JsonNode?> property in jsonNode.Where(static property => property.Value is JsonObject))
        {
            extension.Add(property.Key, LanguageInformation.Parse(property.Value!));
        }

        return extension;
    }

    public object Clone()
    {
        LanguagesInformation result = [];
        foreach (KeyValuePair<string, LanguageInformation> entry in this)
        {
            result.Add(entry.Key, entry.Value);// records don't need to be cloned as they are immutable
        }

        return result;
    }

    public void SerializeAsV31(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        foreach (KeyValuePair<string, LanguageInformation> entry in this.OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            writer.WriteRequiredObject(entry.Key, entry.Value, (w, x) => x.SerializeAsV3(w));
        }
        writer.WriteEndObject();
    }
}
