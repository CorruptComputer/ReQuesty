using System.Text.Json.Nodes;
using ReQuesty.Builder.OpenApiExtensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace ReQuesty.Builder.Extensions;

public static class OpenApiSettingsExtensions
{
    /// <summary>
    /// Adds the OpenAPI extensions used for plugins generation.
    /// </summary>
    public static void AddPluginsExtensions(this OpenApiReaderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.ExtensionParsers ??= new Dictionary<string, Func<JsonNode, OpenApiSpecVersion, IOpenApiExtension>>(StringComparer.OrdinalIgnoreCase);
        settings.ExtensionParsers.TryAdd(OpenApiLogoExtension.Name, static (i, _) => OpenApiLogoExtension.Parse(i));
        settings.ExtensionParsers.TryAdd(OpenApiDescriptionForModelExtension.Name, static (i, _) => OpenApiDescriptionForModelExtension.Parse(i));
        settings.ExtensionParsers.TryAdd(OpenApiPrivacyPolicyUrlExtension.Name, static (i, _) => OpenApiPrivacyPolicyUrlExtension.Parse(i));
        settings.ExtensionParsers.TryAdd(OpenApiLegalInfoUrlExtension.Name, static (i, _) => OpenApiLegalInfoUrlExtension.Parse(i));
    }

    public static void AddGenerationExtensions(this OpenApiReaderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.AddMicrosoftExtensionParsers();
        settings.ExtensionParsers ??= new Dictionary<string, Func<JsonNode, OpenApiSpecVersion, IOpenApiExtension>>(StringComparer.OrdinalIgnoreCase);
        settings.ExtensionParsers.TryAdd(OpenApiReQuestyExtension.Name, static (i, _) => OpenApiReQuestyExtension.Parse(i));
    }

    public static HashSet<string> ReQuestySupportedExtensions()
    {
        OpenApiReaderSettings dummySettings = new();
        dummySettings.AddGenerationExtensions();
        dummySettings.AddPluginsExtensions();

        HashSet<string> supportedExtensions = dummySettings.ExtensionParsers?.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        supportedExtensions.Add("x-openai-isConsequential");// add extension we don't parse to the list

        return supportedExtensions;
    }
}
