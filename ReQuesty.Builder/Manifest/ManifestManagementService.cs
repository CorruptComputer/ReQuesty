using System.Text.Json;
using Microsoft.OpenApi.ApiManifest;

namespace ReQuesty.Builder.Manifest;
/// <summary>
/// Service used to open and decode an API manifest document.
/// </summary>
public class ManifestManagementService
{
    /// <summary>
    /// Deserializes the API manifest document from a JSON representation.
    /// </summary>
    /// <param name="jsonValue">The API manifest JSON representation</param>
    /// <returns>The deserialized manifest</returns>
    public async Task<ApiManifestDocument?> DeserializeManifestDocumentAsync(Stream jsonValue)
    {
        ArgumentNullException.ThrowIfNull(jsonValue);
        JsonDocument jsonDocument = await JsonDocument.ParseAsync(jsonValue).ConfigureAwait(false);
        return ApiManifestDocument.Load(jsonDocument.RootElement);
    }
    /// <summary>
    /// Serializes the API manifest document to a JSON representation.
    /// </summary>
    /// <param name="manifestDocument">The API manifest document to serialize</param>
    /// <param name="stream">The stream to write the serialized JSON representation</param>
    /// <returns>The serialized JSON representation</returns>
    public async Task SerializeManifestDocumentAsync(ApiManifestDocument manifestDocument, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(manifestDocument);
        await using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });
        manifestDocument.Write(writer);
        await writer.FlushAsync().ConfigureAwait(false);
    }
}
