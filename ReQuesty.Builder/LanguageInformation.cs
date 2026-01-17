using System.Text.Json;
using System.Text.Json.Nodes;
using ReQuesty.Builder.Extensions;
using Microsoft.OpenApi;

namespace ReQuesty.Builder;

public record LanguageInformation : IOpenApiSerializable
{
    public List<LanguageDependency> Dependencies { get; set; } = [];
    public string DependencyInstallCommand { get; set; } = string.Empty;
    public string ClientClassName { get; set; } = string.Empty;
    public string ClientNamespaceName { get; set; } = string.Empty;
    public HashSet<string> StructuredMimeTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public void SerializeAsV2(IOpenApiWriter writer)
        => SerializeInternal(writer, static (w, x) => x.SerializeAsV2(w));
    public void SerializeAsV3(IOpenApiWriter writer)
        => SerializeInternal(writer, static (w, x) => x.SerializeAsV3(w));
    public void SerializeAsV31(IOpenApiWriter writer)
        => SerializeInternal(writer, static (w, x) => x.SerializeAsV31(w));
    public void SerializeAsV32(IOpenApiWriter writer)
        => SerializeInternal(writer, static (w, x) => x.SerializeAsV32(w));

    public void SerializeInternal(IOpenApiWriter writer, Action<IOpenApiWriter, LanguageDependency> callback)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        writer.WriteProperty(nameof(DependencyInstallCommand).ToFirstCharacterLowerCase(), DependencyInstallCommand);
        writer.WriteOptionalCollection(nameof(Dependencies).ToFirstCharacterLowerCase(), Dependencies, callback);
        writer.WriteProperty(nameof(ClientClassName).ToFirstCharacterLowerCase(), ClientClassName);
        writer.WriteProperty(nameof(ClientNamespaceName).ToFirstCharacterLowerCase(), ClientNamespaceName);
        writer.WriteOptionalCollection(nameof(StructuredMimeTypes).ToFirstCharacterLowerCase(), StructuredMimeTypes, static (w, x) => { if (!string.IsNullOrEmpty(x)) { w.WriteValue(x); } });
        writer.WriteEndObject();
    }
    public static LanguageInformation Parse(JsonNode source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.GetValueKind() is not JsonValueKind.Object ||
        source.AsObject() is not JsonObject rawObject)
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        LanguageInformation extension = new();
        if (rawObject.TryGetPropertyValue(nameof(Dependencies).ToFirstCharacterLowerCase(), out JsonNode? dependencies) && dependencies is JsonArray arrayValue)
        {
            foreach (JsonNode? entry in arrayValue)
            {
                if (entry is not null)
                {
                    extension.Dependencies.Add(LanguageDependency.Parse(entry));
                }
            }
        }
        if (rawObject.TryGetPropertyValue(nameof(DependencyInstallCommand).ToFirstCharacterLowerCase(), out JsonNode? installCommand) && installCommand is JsonValue stringValue)
        {
            extension.DependencyInstallCommand = stringValue.GetValue<string>();
        }
        // not parsing the maturity level on purpose, we don't want APIs to be able to change that
        if (rawObject.TryGetPropertyValue(nameof(ClientClassName).ToFirstCharacterLowerCase(), out JsonNode? clientClassName) && clientClassName is JsonValue clientClassNameValue)
        {
            extension.ClientClassName = clientClassNameValue.GetValue<string>();
        }
        if (rawObject.TryGetPropertyValue(nameof(ClientNamespaceName).ToFirstCharacterLowerCase(), out JsonNode? clientNamespaceName) && clientNamespaceName is JsonValue clientNamespaceNameValue)
        {
            extension.ClientNamespaceName = clientNamespaceNameValue.GetValue<string>();
        }
        if (rawObject.TryGetPropertyValue(nameof(StructuredMimeTypes).ToFirstCharacterLowerCase(), out JsonNode? structuredMimeTypes) && structuredMimeTypes is JsonArray structuredMimeTypesValue)
        {
            foreach (JsonValue entry in structuredMimeTypesValue.OfType<JsonValue>())
            {
                extension.StructuredMimeTypes.Add(entry.GetValue<string>());
            }
        }
        return extension;
    }
}
public record LanguageDependency : IOpenApiSerializable
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;

    public void SerializeAsV2(IOpenApiWriter writer)
        => SerializeInternal(writer);
    public void SerializeAsV3(IOpenApiWriter writer)
        => SerializeInternal(writer);
    public void SerializeAsV31(IOpenApiWriter writer)
        => SerializeInternal(writer);
    public void SerializeAsV32(IOpenApiWriter writer)
        => SerializeInternal(writer);

    public void SerializeInternal(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        writer.WriteProperty(nameof(Name).ToFirstCharacterLowerCase(), Name);
        writer.WriteProperty(nameof(Version).ToFirstCharacterLowerCase(), Version);
        writer.WriteEndObject();
    }
    public static LanguageDependency Parse(JsonNode source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.GetValueKind() is not JsonValueKind.Object ||
        source.AsObject() is not JsonObject rawObject)
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        LanguageDependency extension = new();
        if (rawObject.TryGetPropertyValue(nameof(Name).ToFirstCharacterLowerCase(), out JsonNode? nameNode) && nameNode is JsonValue nameJsonValue && nameJsonValue.TryGetValue<string>(out string? nameValue))
        {
            extension.Name = nameValue;
        }
        if (rawObject.TryGetPropertyValue(nameof(Version).ToFirstCharacterLowerCase(), out JsonNode? versionNode) && versionNode is JsonValue versionJsonValue && versionJsonValue.TryGetValue<string>(out string? versionValue))
        {
            extension.Version = versionValue;
        }
        return extension;
    }
}