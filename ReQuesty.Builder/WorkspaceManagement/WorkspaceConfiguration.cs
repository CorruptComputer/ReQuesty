using System.Text.Json.Serialization;

namespace ReQuesty.Builder.WorkspaceManagement;

public class WorkspaceConfiguration : ICloneable
{
    /// <summary>
    ///   The version of the configuration file schema.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    ///   The clients to generate.
    /// </summary>
    public Dictionary<string, ApiClientConfiguration> Clients { get; set; } = new Dictionary<string, ApiClientConfiguration>(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, ApiPluginConfiguration> Plugins { get; set; } = new Dictionary<string, ApiPluginConfiguration>(StringComparer.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool AreConsumersKeysUnique
    {
        get
        {
            return Clients.Keys.Concat(Plugins.Keys).GroupBy(static x => x, StringComparer.OrdinalIgnoreCase).All(static x => x.Count() == 1);
        }
    }

    [JsonIgnore]
    public bool AnyConsumerPresent => Clients.Count != 0 || Plugins.Count != 0;

    public object Clone()
    {
        return new WorkspaceConfiguration
        {
            Version = Version,
            Clients = Clients.ToDictionary(static x => x.Key, static x => (ApiClientConfiguration)x.Value.Clone(), StringComparer.OrdinalIgnoreCase),
            Plugins = Plugins.ToDictionary(static x => x.Key, static x => (ApiPluginConfiguration)x.Value.Clone(), StringComparer.OrdinalIgnoreCase)
        };
    }
}
