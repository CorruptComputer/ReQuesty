using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.Json;
using ReQuesty.Extension;
using ReQuesty.Builder;
using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Extensions;
using Microsoft.Extensions.Logging;
using ReQuesty.Consts;

namespace ReQuesty.Handlers;

internal class ReQuestyGenerateCommandHandler : BaseReQuestyCommandHandler
{
    public override async Task<int> InvokeAsync(ParseResult context, CancellationToken cancellationToken)
    {
        // Get options
        string? output = context.GetValue<string?>(CommandLineOptions.OutputOption);
        string? openapi = context.GetValue<string?>(CommandLineOptions.DescriptionOption);
        string? manifest = context.GetValue<string?>(CommandLineOptions.ManifestOption);
        bool backingStore = context.GetValue<bool>(CommandLineOptions.BackingStoreOption);
        bool excludeBackwardCompatible = context.GetValue<bool>(CommandLineOptions.ExcludeBackwardCompatibleOption);
        bool clearCache = context.GetValue<bool>(CommandLineOptions.ClearCacheOption);
        bool disableSSLValidation = context.GetValue<bool>(CommandLineOptions.DisableSSLValidationOption);
        bool includeAdditionalData = context.GetValue<bool>(CommandLineOptions.AdditionalDataOption);
        string? className = context.GetValue<string?>(CommandLineOptions.ClassNameOption);
        AccessModifier typeAccessModifier = context.GetValue<AccessModifier>(CommandLineOptions.TypeAccessModifierOption);
        string? namespaceName = context.GetValue<string?>(CommandLineOptions.NamespaceNameOption);
        List<string> serializer = context.GetValue<List<string>>(CommandLineOptions.SerializerOption).OrEmpty();
        List<string> deserializer = context.GetValue<List<string>>(CommandLineOptions.DeserializerOption).OrEmpty();
        List<string>? includePatterns0 = context.GetValue<List<string>?>(CommandLineOptions.IncludePathOption);
        List<string>? excludePatterns0 = context.GetValue<List<string>?>(CommandLineOptions.ExcludePathOption);
        List<string>? disabledValidationRules0 = context.GetValue<List<string>?>(CommandLineOptions.DisableValidationRulesOption);
        bool cleanOutput = context.GetValue<bool>(CommandLineOptions.CleanOutputOption);
        List<string>? structuredMimeTypes0 = context.GetValue<List<string>?>(CommandLineOptions.StructuredMimeTypesOption);
        LogLevel? logLevel = context.GetValue<LogLevel?>(CommandLineOptions.LogLevelOption);

        List<string> includePatterns = includePatterns0.OrEmpty();
        List<string> excludePatterns = excludePatterns0.OrEmpty();
        List<string> disabledValidationRules = disabledValidationRules0.OrEmpty();
        List<string> structuredMimeTypes = structuredMimeTypes0.OrEmpty();
        AssignIfNotNullOrEmpty(output, (c, s) => c.OutputPath = s);
        AssignIfNotNullOrEmpty(openapi, (c, s) => c.OpenAPIFilePath = s);
        AssignIfNotNullOrEmpty(manifest, (c, s) => c.ApiManifestPath = s);
        AssignIfNotNullOrEmpty(className, (c, s) => c.ClientClassName = s);
        AssignIfNotNullOrEmpty(namespaceName, (c, s) => c.ClientNamespaceName = s);
        Configuration.Generation.TypeAccessModifier = typeAccessModifier;
        Configuration.Generation.UsesBackingStore = backingStore;
        Configuration.Generation.ExcludeBackwardCompatible = excludeBackwardCompatible;
        Configuration.Generation.IncludeAdditionalData = includeAdditionalData;
        Configuration.Generation.Language = GenerationLanguage.CSharp;

        if (serializer.Count != 0)
        {
            Configuration.Generation.Serializers = serializer.Select(static x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        if (deserializer.Count != 0)
        {
            Configuration.Generation.Deserializers = deserializer.Select(static x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        if (includePatterns.Count != 0)
        {
            Configuration.Generation.IncludePatterns = includePatterns.Select(static x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        if (excludePatterns.Count != 0)
        {
            Configuration.Generation.ExcludePatterns = excludePatterns.Select(static x => x.TrimQuotes()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        if (disabledValidationRules.Count != 0)
        {
            Configuration.Generation.DisabledValidationRules = disabledValidationRules
                                                                    .Select(static x => x.TrimQuotes())
                                                                    .SelectMany(static x => x.Split(',', StringSplitOptions.RemoveEmptyEntries))
                                                                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        if (structuredMimeTypes.Count != 0)
        {
            Configuration.Generation.StructuredMimeTypes = new(structuredMimeTypes.SelectMany(static x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                                                            .Select(static x => x.TrimQuotes()));
        }

        Configuration.Generation.OpenAPIFilePath = GetAbsolutePath(Configuration.Generation.OpenAPIFilePath);
        Configuration.Generation.OutputPath = NormalizeSlashesInPath(GetAbsolutePath(Configuration.Generation.OutputPath));
        Configuration.Generation.ApiManifestPath = NormalizeSlashesInPath(GetAbsolutePath(Configuration.Generation.ApiManifestPath));
        Configuration.Generation.CleanOutput = cleanOutput;
        Configuration.Generation.ClearCache = clearCache;
        Configuration.Generation.DisableSSLValidation = disableSSLValidation;

        (ILoggerFactory loggerFactory, ILogger<ReQuestyBuilder> logger) = GetLoggerAndFactory<ReQuestyBuilder>(context, Configuration.Generation.OutputPath);
        using (loggerFactory)
        {
            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration, ReQuestyConfigurationJsonContext.Default.ReQuestyConfiguration));

            try
            {
                ReQuestyBuilder builder = new(logger, Configuration.Generation, httpClient);
                bool result = await builder.GenerateClientAsync(cancellationToken).ConfigureAwait(false);
                if (result)
                {
                    DisplaySuccess("Generation completed successfully");
                    DisplayUrlInformation(Configuration.Generation.ApiRootUrl);
                }
                else
                {
                    DisplaySuccess("Generation skipped as no changes were detected");
                }
                Tuple<string, IEnumerable<string>>? manifestResult = await builder.GetApiManifestDetailsAsync(true, cancellationToken).ConfigureAwait(false);
                string manifestPath = manifestResult is null ? string.Empty : Configuration.Generation.ApiManifestPath;
                return 0;
            }
            catch (Exception ex)
            {
#if DEBUG
                logger.LogCritical(ex, "error generating the client: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
#else
                logger.LogCritical("error generating the client: {exceptionMessage}", ex.Message);
                return 1;
#endif
            }
        }
    }
}
