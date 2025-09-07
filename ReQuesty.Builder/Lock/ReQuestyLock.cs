using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Configuration;

namespace ReQuesty.Builder.Lock;

/// <summary>
/// A class that represents a lock file for a ReQuesty project.
/// </summary>
public class ReQuestyLock
{
    /// <summary>
    /// The OpenAPI description hash that generated this client.
    /// </summary>
    public string DescriptionHash { get; set; } = string.Empty;
    /// <summary>
    /// The location of the OpenAPI description file.
    /// </summary>
    public string DescriptionLocation { get; set; } = string.Empty;
    /// <summary>
    /// The version of the lock file schema.
    /// </summary>
    public string LockFileVersion { get; set; } = "1.0.0";
    /// <summary>
    /// The version of the ReQuesty generator that generated this client.
    /// </summary>
    public string ReQuestyVersion { get; set; } = ReQuesty.Generated.ReQuestyVersion.Current();
    /// <summary>
    /// The main class name for this client.
    /// </summary>
    public string ClientClassName { get; set; } = string.Empty;
    /// <summary>
    /// The type access modifier to use for the client types.
    /// </summary>
    public string TypeAccessModifier { get; set; } = "Public";
    /// <summary>
    /// The main namespace for this client.
    /// </summary>
    public string ClientNamespaceName { get; set; } = string.Empty;
    /// <summary>
    /// The language for this client.
    /// </summary>
    public string Language { get; set; } = string.Empty;
    /// <summary>
    /// Whether the backing store was used for this client.
    /// </summary>
    public bool UsesBackingStore
    {
        get; set;
    }
    /// <summary>
    /// Whether backward compatible code was excluded for this client.
    /// </summary>
    public bool ExcludeBackwardCompatible
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
    /// Whether SSL Validation was disabled for this client.
    /// </summary>
    public bool DisableSSLValidation
    {
        get; set;
    }
#pragma warning disable CA2227
    /// <summary>
    /// The serializers used for this client.
    /// </summary>
    public HashSet<string> Serializers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The deserializers used for this client.
    /// </summary>
    public HashSet<string> Deserializers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The structured mime types used for this client.
    /// </summary>
#pragma warning disable CA1002
    public List<string> StructuredMimeTypes { get; set; } = [];
#pragma warning restore CA1002
    /// <summary>
    /// The path patterns for API endpoints to include for this client.
    /// </summary>
    public HashSet<string> IncludePatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The path patterns for API endpoints to exclude for this client.
    /// </summary>
    public HashSet<string> ExcludePatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The OpenAPI validation rules to disable during the generation.
    /// </summary>
    public HashSet<string> DisabledValidationRules { get; set; } = new(StringComparer.OrdinalIgnoreCase);
#pragma warning restore CA2227
    /// <summary>
    /// Updates the passed configuration with the values from the lock file.
    /// </summary>
    /// <param name="config">The configuration to update.</param>
    public void UpdateGenerationConfigurationFromLock(GenerationConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.ClientClassName = ClientClassName;
        if (Enum.TryParse<GenerationLanguage>(Language, out GenerationLanguage parsedLanguage))
        {
            config.Language = parsedLanguage;
        }

        config.ClientNamespaceName = ClientNamespaceName;
        if (Enum.TryParse<AccessModifier>(TypeAccessModifier, out AccessModifier parsedAccessModifier))
        {
            config.TypeAccessModifier = parsedAccessModifier;
        }

        config.UsesBackingStore = UsesBackingStore;
        config.IncludeAdditionalData = IncludeAdditionalData;
        config.Serializers = Serializers.ToHashSet(StringComparer.OrdinalIgnoreCase);
        config.Deserializers = Deserializers.ToHashSet(StringComparer.OrdinalIgnoreCase);
        config.StructuredMimeTypes = new(StructuredMimeTypes);
        config.IncludePatterns = IncludePatterns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        config.ExcludePatterns = ExcludePatterns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        config.OpenAPIFilePath = DescriptionLocation;
        config.DisabledValidationRules = DisabledValidationRules.ToHashSet(StringComparer.OrdinalIgnoreCase);
        config.DisableSSLValidation = DisableSSLValidation;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="ReQuestyLock"/> class.
    /// </summary>
    public ReQuestyLock()
    {
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="ReQuestyLock"/> class from the passed configuration.
    /// </summary>
    /// <param name="config">The configuration to use.</param>
    public ReQuestyLock(GenerationConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        Language = config.Language.ToString();
        ClientClassName = config.ClientClassName;
        TypeAccessModifier = config.TypeAccessModifier.ToString();
        ClientNamespaceName = config.ClientNamespaceName;
        UsesBackingStore = config.UsesBackingStore;
        IncludeAdditionalData = config.IncludeAdditionalData;
        Serializers = config.Serializers.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Deserializers = config.Deserializers.ToHashSet(StringComparer.OrdinalIgnoreCase);
        StructuredMimeTypes = config.StructuredMimeTypes.ToList();
        IncludePatterns = config.IncludePatterns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        ExcludePatterns = config.ExcludePatterns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        DescriptionLocation = config.OpenAPIFilePath;
        DisabledValidationRules = config.DisabledValidationRules.ToHashSet(StringComparer.OrdinalIgnoreCase);
        DisableSSLValidation = config.DisableSSLValidation;
    }
}
