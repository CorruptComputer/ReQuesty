using System.Text.Json.Nodes;
using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Extensions;
using ReQuesty.Builder.Lock;
using Microsoft.OpenApi.ApiManifest;

namespace ReQuesty.Builder.Configuration;

#pragma warning disable CA2227
#pragma warning disable CA1056
public class GenerationConfiguration : ICloneable
{
    public static GenerationConfiguration DefaultConfiguration
    {
        get;
    } = new();
    public bool ShouldGetApiManifest
    {
        get
        {
            return (string.IsNullOrEmpty(OpenAPIFilePath) || OpenAPIFilePath.Equals(DefaultConfiguration.OpenAPIFilePath, StringComparison.OrdinalIgnoreCase)) &&
                (!string.IsNullOrEmpty(ApiManifestPath) || !ApiManifestPath.Equals(DefaultConfiguration.ApiManifestPath, StringComparison.OrdinalIgnoreCase)) &&
                (ApiManifestPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) || File.Exists(ApiManifestPath));
        }
    }
    public bool SkipGeneration
    {
        get; set;
    }
    public ConsumerOperation? Operation
    {
        get; set;
    }
    public string OpenAPIFilePath { get; set; } = "openapi.yaml";
    public string ApiManifestPath { get; set; } = "apimanifest.json";
    // Optional filename suffix to be used when generating multiple API plugins for the same OpenAPI file.
    // Note: It can not be set from the outside, it is only used internally when generating the plugin manifest.
    internal string FileNameSuffix { get; set; } = "";

    public string OutputPath { get; set; } = "./output";
    public string ClientClassName { get; set; } = "ApiClient";
    public AccessModifier TypeAccessModifier { get; set; } = AccessModifier.Public;
    public string ClientNamespaceName { get; set; } = "ApiSdk";
    public string NamespaceNameSeparator { get; set; } = ".";
    public bool ExportPublicApi
    {
        get; set;
    }
    internal const string ModelsNamespaceSegmentName = "models";
    public string ModelsNamespaceName
    {
        get => $"{ClientNamespaceName}{NamespaceNameSeparator}{ModelsNamespaceSegmentName}";
    }
    public GenerationLanguage Language { get; set; } = GenerationLanguage.CSharp;
    public HashSet<PluginType> PluginTypes { get; set; } = [];
    public string? ApiRootUrl
    {
        get; set;
    }
    public bool UsesBackingStore
    {
        get; set;
    }
    public bool IncludeAdditionalData { get; set; } = true;
    public HashSet<string> Serializers
    {
        get; set;
    } = new(4, StringComparer.OrdinalIgnoreCase){
        "ReQuesty.Runtime.Serialization.Json.JsonSerializationWriterFactory",
        "ReQuesty.Runtime.Serialization.Text.TextSerializationWriterFactory",
        "ReQuesty.Runtime.Serialization.Form.FormSerializationWriterFactory",
        "ReQuesty.Runtime.Serialization.Multipart.MultipartSerializationWriterFactory"
    };
    public HashSet<string> Deserializers
    {
        get; set;
    } = new(3, StringComparer.OrdinalIgnoreCase) {
        "ReQuesty.Runtime.Serialization.Json.JsonParseNodeFactory",
        "ReQuesty.Runtime.Serialization.Text.TextParseNodeFactory",
        "ReQuesty.Runtime.Serialization.Form.FormParseNodeFactory",
    };
    public bool ShouldWriteBarrelsIfClassExists
    {
        get
        {
            return BarreledLanguagesWithConstantFileName.Contains(Language);
        }
    }
    private static readonly HashSet<GenerationLanguage> BarreledLanguagesWithConstantFileName = [];
    public bool CleanOutput
    {
        get; set;
    }
    public StructuredMimeTypesCollection StructuredMimeTypes
    {
        get; set;
    } = [
        "application/json;q=1",
        "text/plain;q=0.9",
        "application/x-www-form-urlencoded;q=0.2",
        "multipart/form-data;q=0.1",
    ];
    public HashSet<string> IncludePatterns { get; set; } = new(0, StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ExcludePatterns { get; set; } = new(0, StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The overrides loaded from the api manifest when refreshing a client, as opposed to the user provided ones.
    /// </summary>
    public HashSet<string> PatternsOverride { get; set; } = new(0, StringComparer.OrdinalIgnoreCase);
    public bool ClearCache
    {
        get; set;
    }
    public HashSet<string> DisabledValidationRules { get; set; } = new(0, StringComparer.OrdinalIgnoreCase);
    public bool? IncludeReQuestyValidationRules
    {
        get; set;
    }

    // If set to true, this allows to parse extensions from manifest
    // to use in query operations for RPC requests
    public bool? IncludePluginExtensions
    {
        get; set;
    }

    public bool NoWorkspace
    {
        get; set;
    }

    public int MaxDegreeOfParallelism { get; set; } = -1;
    public object Clone()
    {
        return new GenerationConfiguration
        {
            OpenAPIFilePath = OpenAPIFilePath,
            OutputPath = OutputPath,
            ClientClassName = ClientClassName,
            ClientNamespaceName = ClientNamespaceName,
            NamespaceNameSeparator = NamespaceNameSeparator,
            Language = Language,
            ApiRootUrl = ApiRootUrl,
            UsesBackingStore = UsesBackingStore,
            IncludeAdditionalData = IncludeAdditionalData,
            Serializers = new(Serializers ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase),
            Deserializers = new(Deserializers ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase),
            CleanOutput = CleanOutput,
            StructuredMimeTypes = new(StructuredMimeTypes ?? Enumerable.Empty<string>()),
            IncludePatterns = new(IncludePatterns ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase),
            ExcludePatterns = new(ExcludePatterns ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase),
            ClearCache = ClearCache,
            DisabledValidationRules = new(DisabledValidationRules ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase),
            IncludeReQuestyValidationRules = IncludeReQuestyValidationRules,
            IncludePluginExtensions = IncludePluginExtensions,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            SkipGeneration = SkipGeneration,
            NoWorkspace = NoWorkspace,
            Operation = Operation,
            PatternsOverride = new(PatternsOverride ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase),
            PluginTypes = new(PluginTypes ?? Enumerable.Empty<PluginType>()),
            DisableSSLValidation = DisableSSLValidation,
            ExportPublicApi = ExportPublicApi,
        };
    }
    private static readonly StringIEnumerableDeepComparer comparer = new();
    internal void UpdateConfigurationFromLanguagesInformation(LanguagesInformation languagesInfo)
    {
        if (!languagesInfo.TryGetValue(Language.ToString(), out LanguageInformation? languageInfo))
        {
            return;
        }

        GenerationConfiguration defaultConfiguration = new();
        if (!string.IsNullOrEmpty(languageInfo.ClientClassName) &&
            ClientClassName.Equals(defaultConfiguration.ClientClassName, StringComparison.Ordinal) &&
            !ClientClassName.Equals(languageInfo.ClientClassName, StringComparison.Ordinal))
        {
            ClientClassName = languageInfo.ClientClassName;
        }

        if (!string.IsNullOrEmpty(languageInfo.ClientNamespaceName) &&
            ClientNamespaceName.Equals(defaultConfiguration.ClientNamespaceName, StringComparison.Ordinal) &&
            !ClientNamespaceName.Equals(languageInfo.ClientNamespaceName, StringComparison.Ordinal))
        {
            ClientNamespaceName = languageInfo.ClientNamespaceName;
        }

        if (languageInfo.StructuredMimeTypes.Count != 0 &&
            comparer.Equals(StructuredMimeTypes, defaultConfiguration.StructuredMimeTypes) &&
            !comparer.Equals(languageInfo.StructuredMimeTypes, StructuredMimeTypes))
        {
            StructuredMimeTypes = new(languageInfo.StructuredMimeTypes);
        }
    }
    public const string ReQuestyHashManifestExtensionKey = "x-requesty-hash";
    public const string ReQuestyVersionManifestExtensionKey = "x-requesty-version";
    public ApiDependency ToApiDependency(string configurationHash, Dictionary<string, HashSet<string>> templatesWithOperations, string targetDirectory)
    {
        ApiDependency dependency = new()
        {
            ApiDescriptionUrl = NormalizeDescriptionLocation(targetDirectory),
            ApiDeploymentBaseUrl = ApiRootUrl?.EndsWith('/') ?? false ? ApiRootUrl : $"{ApiRootUrl}/",
            Extensions = [],
            Requests = templatesWithOperations.SelectMany(static x => x.Value.Select(y => new RequestInfo { Method = y.ToUpperInvariant(), UriTemplate = x.Key.DeSanitizeUrlTemplateParameter() })).ToList(),
        };

        if (!string.IsNullOrEmpty(configurationHash))
        {
            dependency.Extensions.Add(ReQuestyHashManifestExtensionKey, JsonValue.Create(configurationHash));// only include non empty value.
        }
        dependency.Extensions.Add(ReQuestyVersionManifestExtensionKey, ReQuesty.Generated.ReQuestyVersion.Current());
        return dependency;
    }
    private string NormalizeDescriptionLocation(string targetDirectory)
    {
        if (!string.IsNullOrEmpty(OpenAPIFilePath) &&
            !string.IsNullOrEmpty(targetDirectory) &&
            !OpenAPIFilePath.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
            Path.IsPathRooted(OpenAPIFilePath) &&
            Path.GetFullPath(OpenAPIFilePath).StartsWith(Path.GetFullPath(targetDirectory), StringComparison.Ordinal))
        {
            return "./" + Path.GetRelativePath(targetDirectory, OpenAPIFilePath).NormalizePathSeparators();
        }

        return OpenAPIFilePath;
    }
    public bool IsPluginConfiguration => PluginTypes.Count != 0;

    public bool DisableSSLValidation
    {
        get; set;
    }
}
#pragma warning restore CA1056
#pragma warning restore CA2227
