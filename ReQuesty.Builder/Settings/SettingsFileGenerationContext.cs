using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReQuesty.Builder.Settings;

[JsonSerializable(typeof(SettingsFile))]
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
internal partial class SettingsFileGenerationContext : JsonSerializerContext
{
}
