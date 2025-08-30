using ReQuesty.Builder;
using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Configuration;
using Microsoft.Extensions.Configuration;

namespace ReQuesty;

internal static class ReQuestyConfigurationExtensions
{
    /// <summary>
    /// Binds the configuration to the ReQuestyConfiguration object
    /// This implementation is a workaround for the fact that Configuration.Bind uses reflection and is not trimmable
    /// <see href="https://github.com/dotnet/runtime/issues/36130"/>
    /// </summary>
    /// <param name="configObject">The configuration object to bind to</param>
    /// <param name="configuration">The configuration to bind from</param>
    public static void BindConfiguration(this ReQuestyConfiguration configObject, IConfigurationRoot configuration)
    {
        ArgumentNullException.ThrowIfNull(configObject);
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection languagesSection = configuration.GetSection(nameof(configObject.Languages));
        foreach (IConfigurationSection section in languagesSection.GetChildren())
        {
            LanguageInformation lngInfo = new()
            {
                ClientClassName = section[nameof(LanguageInformation.ClientClassName)] ?? string.Empty,
                ClientNamespaceName = section[nameof(LanguageInformation.ClientNamespaceName)] ?? string.Empty,
                DependencyInstallCommand = section[nameof(LanguageInformation.DependencyInstallCommand)] ?? string.Empty,
                MaturityLevel = Enum.TryParse<LanguageMaturityLevel>(section[nameof(LanguageInformation.MaturityLevel)], true, out LanguageMaturityLevel ml) ? ml : LanguageMaturityLevel.Experimental,
                SupportExperience = Enum.TryParse<SupportExperience>(section[nameof(LanguageInformation.SupportExperience)], true, out SupportExperience se) ? se : SupportExperience.Community,
            };
            section.GetSection(nameof(lngInfo.StructuredMimeTypes)).LoadHashSet(lngInfo.StructuredMimeTypes);
            IConfigurationSection dependenciesSection = section.GetSection(nameof(lngInfo.Dependencies));
            foreach (IConfigurationSection dependency in dependenciesSection.GetChildren())
            {
                lngInfo.Dependencies.Add(new LanguageDependency
                {
                    Version = dependency[nameof(LanguageDependency.Version)] ?? string.Empty,
                    Name = dependency[nameof(LanguageDependency.Name)] ?? string.Empty,
                });
            }
            configObject.Languages.Add(section.Key, lngInfo);
        }
        configObject.Generation.Language = Enum.TryParse<GenerationLanguage>(configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.Language)}"], true, out GenerationLanguage language) ? language : GenerationLanguage.CSharp;
        configObject.Generation.OpenAPIFilePath = configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.OpenAPIFilePath)}"] is string openApiFilePath && !string.IsNullOrEmpty(openApiFilePath) ? openApiFilePath : configObject.Generation.OpenAPIFilePath;
        configObject.Generation.OutputPath = configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.OutputPath)}"] is string outputPath && !string.IsNullOrEmpty(outputPath) ? outputPath : configObject.Generation.OutputPath;
        configObject.Generation.ClientClassName = configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.ClientClassName)}"] is string clientClassName && !string.IsNullOrEmpty(clientClassName) ? clientClassName : configObject.Generation.ClientClassName;
        configObject.Generation.TypeAccessModifier = Enum.TryParse<AccessModifier>(configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.TypeAccessModifier)}"], true, out AccessModifier accessModifier) ? accessModifier : AccessModifier.Public;
        configObject.Generation.ClientNamespaceName = configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.ClientNamespaceName)}"] is string clientNamespaceName && !string.IsNullOrEmpty(clientNamespaceName) ? clientNamespaceName : configObject.Generation.ClientNamespaceName;
        configObject.Generation.UsesBackingStore = bool.TryParse(configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.UsesBackingStore)}"], out bool usesBackingStore) && usesBackingStore;
        configObject.Generation.IncludeAdditionalData = bool.TryParse(configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.IncludeAdditionalData)}"], out bool includeAdditionalData) && includeAdditionalData;
        configObject.Generation.CleanOutput = bool.TryParse(configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.CleanOutput)}"], out bool cleanOutput) && cleanOutput;
        configObject.Generation.ClearCache = bool.TryParse(configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.ClearCache)}"], out bool clearCache) && clearCache;
        configObject.Generation.ExcludeBackwardCompatible = bool.TryParse(configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.ExcludeBackwardCompatible)}"], out bool excludeBackwardCompatible) && excludeBackwardCompatible;
        configObject.Generation.MaxDegreeOfParallelism = int.TryParse(configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.MaxDegreeOfParallelism)}"], out int maxDegreeOfParallelism) ? maxDegreeOfParallelism : configObject.Generation.MaxDegreeOfParallelism;
        configObject.Generation.ExportPublicApi = bool.TryParse(configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.ExportPublicApi)}"], out bool exportPublicApi) && exportPublicApi;
        configObject.Generation.NoWorkspace = bool.TryParse(configuration[$"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.NoWorkspace)}"], out bool noWorkspace) && noWorkspace;
        configuration.GetSection($"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.StructuredMimeTypes)}").LoadCollection(configObject.Generation.StructuredMimeTypes);
        configuration.GetSection($"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.Serializers)}").LoadHashSet(configObject.Generation.Serializers);
        configuration.GetSection($"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.Deserializers)}").LoadHashSet(configObject.Generation.Deserializers);
        configuration.GetSection($"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.IncludePatterns)}").LoadHashSet(configObject.Generation.IncludePatterns);
        configuration.GetSection($"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.ExcludePatterns)}").LoadHashSet(configObject.Generation.ExcludePatterns);
        configuration.GetSection($"{nameof(configObject.Generation)}:{nameof(GenerationConfiguration.DisabledValidationRules)}").LoadHashSet(configObject.Generation.DisabledValidationRules);
    }
    private static void LoadCollection(this IConfigurationSection section, StructuredMimeTypesCollection collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (section is null)
        {
            return;
        }

        IEnumerable<IConfigurationSection> children = section.GetChildren();
        if (children.Any() && collection.Count != 0)
        {
            collection.Clear();
        }

        foreach (IConfigurationSection item in children)
        {
            if (section[item.Key] is string value && !string.IsNullOrEmpty(value))
            {
                collection.Add(value);
            }
        }
    }
    private static void LoadHashSet(this IConfigurationSection section, HashSet<string> hashSet)
    {
        ArgumentNullException.ThrowIfNull(hashSet);
        if (section is null)
        {
            return;
        }

        IEnumerable<IConfigurationSection> children = section.GetChildren();
        if (children.Any() && hashSet.Count != 0)
        {
            hashSet.Clear();
        }

        foreach (IConfigurationSection item in children)
        {
            if (section[item.Key] is string value && !string.IsNullOrEmpty(value))
            {
                hashSet.Add(value);
            }
        }
    }
}
