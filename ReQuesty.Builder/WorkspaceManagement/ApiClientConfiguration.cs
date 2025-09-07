using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Configuration;
using Microsoft.OpenApi.ApiManifest;

namespace ReQuesty.Builder.WorkspaceManagement;

#pragma warning disable CA2227 // Collection properties should be read only
public class ApiClientConfiguration : BaseApiConsumerConfiguration, ICloneable
{
    /// <summary>
    /// The language for this client.
    /// </summary>
    public string Language { get; set; } = string.Empty;
    /// <summary>
    /// The type access modifier to use for the client types.
    /// </summary>
    public string TypeAccessModifier { get; set; } = "Public";
    /// <summary>
    /// The structured mime types used for this client.
    /// </summary>
#pragma warning disable CA1002
    public List<string> StructuredMimeTypes { get; set; } = [];
#pragma warning restore CA1002
    /// <summary>
    /// The main namespace for this client.
    /// </summary>
    public string ClientNamespaceName { get; set; } = string.Empty;
    /// <summary>
    /// Whether the backing store was used for this client.
    /// </summary>
    public bool UsesBackingStore
    {
        get; set;
    }
    /// <summary>
    /// Whether additional data was used for this client.
    /// </summary>
    public bool IncludeAdditionalData
    {
        get; set;
    }
    /// <summary>
    /// The OpenAPI validation rules to disable during the generation.
    /// </summary>
    public HashSet<string> DisabledValidationRules { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClientConfiguration"/> class.
    /// </summary>
    public ApiClientConfiguration() : base()
    {

    }
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClientConfiguration"/> class from an existing <see cref="GenerationConfiguration"/>.
    /// </summary>
    /// <param name="config">The configuration to use to initialize the client configuration</param>
    public ApiClientConfiguration(GenerationConfiguration config) : base(config)
    {
        ArgumentNullException.ThrowIfNull(config);
        Language = config.Language.ToString();
        TypeAccessModifier = config.TypeAccessModifier.ToString();
        ClientNamespaceName = config.ClientNamespaceName;
        UsesBackingStore = config.UsesBackingStore;
        IncludeAdditionalData = config.IncludeAdditionalData;
        StructuredMimeTypes = config.StructuredMimeTypes.ToList();
        DisabledValidationRules = config.DisabledValidationRules.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
    /// <summary>
    /// Updates the passed configuration with the values from the config file.
    /// </summary>
    /// <param name="config">Generation configuration to update.</param>
    /// <param name="clientName">Client name serving as class name.</param>
    /// <param name="requests">The requests to use when updating an existing client.</param>
    public void UpdateGenerationConfigurationFromApiClientConfiguration(GenerationConfiguration config, string clientName, IList<RequestInfo>? requests = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrEmpty(clientName);
        config.ClientNamespaceName = ClientNamespaceName;
        if (Enum.TryParse<GenerationLanguage>(Language, out GenerationLanguage parsedLanguage))
        {
            config.Language = parsedLanguage;
        }

        if (Enum.TryParse<AccessModifier>(TypeAccessModifier, out AccessModifier parsedTypeAccessModifier))
        {
            config.TypeAccessModifier = parsedTypeAccessModifier;
        }

        config.UsesBackingStore = UsesBackingStore;
        config.IncludeAdditionalData = IncludeAdditionalData;
        config.StructuredMimeTypes = new(StructuredMimeTypes);
        config.DisabledValidationRules = DisabledValidationRules.ToHashSet(StringComparer.OrdinalIgnoreCase);
        UpdateGenerationConfigurationFromBase(config, clientName, requests);
    }

    public object Clone()
    {
        ApiClientConfiguration result = new()
        {
            Language = Language,
            TypeAccessModifier = TypeAccessModifier,
            StructuredMimeTypes = [.. StructuredMimeTypes],
            ClientNamespaceName = ClientNamespaceName,
            UsesBackingStore = UsesBackingStore,
            IncludeAdditionalData = IncludeAdditionalData,
            DisabledValidationRules = new(DisabledValidationRules, StringComparer.OrdinalIgnoreCase),
        };
        CloneBase(result);
        return result;
    }
}
#pragma warning restore CA2227 // Collection properties should be read only
