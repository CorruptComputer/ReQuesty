using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace ReQuesty.Builder.Configuration;

public class LanguagesInformation : Dictionary<string, LanguageInformation>, IOpenApiSerializable, ICloneable
{
    public void SerializeAsV2(IOpenApiWriter writer)
        => SerializeInternal(writer, static (w, x) => x.SerializeAsV2(w));
    public void SerializeAsV3(IOpenApiWriter writer)
        => SerializeInternal(writer, static (w, x) => x.SerializeAsV3(w));
    public void SerializeAsV31(IOpenApiWriter writer)
        => SerializeInternal(writer, static (w, x) => x.SerializeAsV31(w));
    public void SerializeAsV32(IOpenApiWriter writer)
        => SerializeInternal(writer, static (w, x) => x.SerializeAsV32(w));

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

    private void SerializeInternal(IOpenApiWriter writer, Action<IOpenApiWriter, LanguageInformation> callback)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        foreach (KeyValuePair<string, LanguageInformation> entry in this.OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            writer.WriteRequiredObject(entry.Key, entry.Value, callback);
        }
        writer.WriteEndObject();
    }
}
