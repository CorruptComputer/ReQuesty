using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.RegularExpressions;
using ReQuesty.Handlers;
using ReQuesty.Builder;
using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Validation;
using Microsoft.Extensions.Logging;

namespace ReQuesty;

public static partial class ReQuestyHost
{
    public static RootCommand GetRootCommand()
    {
        RootCommand rootCommand = [];
        rootCommand.AddCommand(GetGenerateCommand());

        return rootCommand;
    }

    internal static Option<bool> GetCleanOutputOption(bool defaultValue)
    {
        Option<bool> cleanOutputOption = new("--clean-output", () => defaultValue, "Removes all files from the output directory before generating the code files.");
        cleanOutputOption.AddAlias("--co");
        return cleanOutputOption;
    }
    internal static Option<string> GetOutputPathOption(string defaultValue)
    {
        Option<string> outputOption = new("--output", () => defaultValue, "The output directory path for the generated code files.");
        outputOption.AddAlias("-o");
        outputOption.ArgumentHelpName = "path";
        return outputOption;
    }
    internal static Option<List<string>> GetDisableValidationRulesOption()
    {
        string parameterName = "--disable-validation-rules";
        Option<List<string>> option = new(parameterName, () => [], "The OpenAPI description validation rules to disable. Accepts multiple values.");
        option.AddAlias("--dvr");
        string[] validationRules = [
                                    nameof(DivergentResponseSchema),
                                    nameof(GetWithBody),
                                    nameof(InconsistentTypeFormatPair),
                                    nameof(KnownAndNotSupportedFormats),
                                    nameof(MissingDiscriminator),
                                    nameof(MultipleServerEntries),
                                    nameof(NoContentWithBody),
                                    nameof(NoServerEntry),
                                    nameof(UrlFormEncodedComplex),
                                    nameof(ValidationRuleSetExtensions),
                                    "All"
                                    ];
        option.AddValidator(x => ValidateKnownValues(x, parameterName, validationRules));
        option.ArgumentHelpName = string.Join(",", validationRules);
        option.Arity = ArgumentArity.ZeroOrMore;
        return option;
    }

    private static readonly Lazy<bool> isRunningInContainer = new(() =>
    {
        string? requestyInContainerRaw = Environment.GetEnvironmentVariable("REQUESTY_CONTAINER");

        return !string.IsNullOrEmpty(requestyInContainerRaw)
            && bool.TryParse(requestyInContainerRaw, out bool requestyInContainer)
            && requestyInContainer;
    });

    internal static Option<string> GetDescriptionOption(string defaultValue, bool isRequired = false)
    {
        Option<string> descriptionOption = new("--openapi", "The path or URI to the OpenAPI description file used to generate the code files.");
        if (isRunningInContainer.Value && !isRequired)
        {
            descriptionOption.SetDefaultValue(defaultValue);
        }

        descriptionOption.AddAlias("-d");
        descriptionOption.ArgumentHelpName = "path";
        descriptionOption.IsRequired = isRequired;
        return descriptionOption;
    }
    private static Option<string> GetManifestOption(string defaultValue)
    {
        Option<string> manifestOption = new("--manifest", "The path or URI to the API manifest file used to generate the code files. Append #apikey if the target manifest contains multiple API dependencies entries.");
        if (isRunningInContainer.Value)
        {
            manifestOption.SetDefaultValue(defaultValue);
        }

        manifestOption.AddAlias("-a");
        return manifestOption;
    }
    [GeneratedRegex(@"^[a-zA-Z_][\w]*", RegexOptions.Singleline, 500)]
    private static partial Regex classNameRegex();
    [GeneratedRegex(@"^[\w][\w\._-]+", RegexOptions.Singleline, 500)]
    private static partial Regex namespaceNameRegex();

    internal static Option<AccessModifier> GetTypeAccessModifierOption()
    {
        Option<AccessModifier> accessOption = new("--type-access-modifier", "The type access modifier to use for the client types.");
        accessOption.AddAlias("--tam");
        accessOption.SetDefaultValue(AccessModifier.Public);
        AddEnumValidator(accessOption, "type-access-modifier");
        return accessOption;
    }

    internal static Option<AccessModifier?> GetOptionalTypeAccessModifierOption()
    {
        Option<AccessModifier?> accessOption = new("--type-access-modifier", "The type access modifier to use for the client types.");
        accessOption.AddAlias("--tam");
        AddEnumValidator(accessOption, "type-access-modifier");
        return accessOption;
    }
    internal static Option<string> GetNamespaceOption(string defaultNamespaceName)
    {
        Option<string> namespaceOption = new("--namespace-name", () => defaultNamespaceName, "The namespace to use for the core client class specified with the --class-name option.");
        namespaceOption.AddAlias("-n");
        namespaceOption.ArgumentHelpName = "name";
        AddStringRegexValidator(namespaceOption, namespaceNameRegex(), "namespace name", string.IsNullOrEmpty(defaultNamespaceName));
        return namespaceOption;
    }
    internal static Option<bool> GetBackingStoreOption(bool defaultValue = false)
    {
        Option<bool> backingStoreOption = new("--backing-store", () => defaultValue, "Enables backing store for models.");
        backingStoreOption.AddAlias("-b");
        return backingStoreOption;
    }
    internal static Option<bool?> GetOptionalBackingStoreOption()
    {
        Option<bool?> backingStoreOption = new("--backing-store", "Enables backing store for models.");
        backingStoreOption.AddAlias("-b");
        return backingStoreOption;
    }
    internal static Option<bool> GetExcludeBackwardCompatibleOption(bool defaultValue = false)
    {
        Option<bool> excludeBackwardCompatible = new("--exclude-backward-compatible", () => defaultValue, "Excludes backward compatible and obsolete assets from the generated result. Should be used for new clients.");
        excludeBackwardCompatible.AddAlias("--ebc");
        return excludeBackwardCompatible;
    }
    internal static Option<bool?> GetOptionalExcludeBackwardCompatibleOption()
    {
        Option<bool?> excludeBackwardCompatible = new("--exclude-backward-compatible", "Excludes backward compatible and obsolete assets from the generated result. Should be used for new clients.");
        excludeBackwardCompatible.AddAlias("--ebc");
        return excludeBackwardCompatible;
    }
    internal static Option<bool> GetAdditionalDataOption(bool defaultValue = true)
    {
        Option<bool> additionalDataOption = new("--additional-data", () => defaultValue, "Will include the 'AdditionalData' property for models.");
        additionalDataOption.AddAlias("--ad");
        return additionalDataOption;
    }
    internal static Option<bool?> GetOptionalAdditionalDataOption()
    {
        Option<bool?> additionalDataOption = new("--additional-data", "Will include the 'AdditionalData' property for models.");
        additionalDataOption.AddAlias("--ad");
        return additionalDataOption;
    }
    internal static Option<List<string>> GetStructuredMimeTypesOption(List<string> defaultValue)
    {
        Option<List<string>> structuredMimeTypesOption = new(
            "--structured-mime-types",
            () => defaultValue,
        "The MIME types with optional priorities as defined in RFC9110 Accept header to use for structured data model generation. Accepts multiple values.");
        structuredMimeTypesOption.AddAlias("-m");
        return structuredMimeTypesOption;
    }
    private static Command GetGenerateCommand()
    {
        GenerationConfiguration defaultConfiguration = new();
        Option<string> descriptionOption = GetDescriptionOption(defaultConfiguration.OpenAPIFilePath);
        Option<string> manifestOption = GetManifestOption(defaultConfiguration.ApiManifestPath);

        Option<string> outputOption = GetOutputPathOption(defaultConfiguration.OutputPath);

        Option<string> classOption = new("--class-name", () => defaultConfiguration.ClientClassName, "The class name to use for the core client class.");
        classOption.AddAlias("-c");
        classOption.ArgumentHelpName = "name";
        AddStringRegexValidator(classOption, classNameRegex(), "class name");

        Option<AccessModifier> typeAccessModifierOption = GetTypeAccessModifierOption();

        Option<string> namespaceOption = GetNamespaceOption(defaultConfiguration.ClientNamespaceName);

        Option<LogLevel> logLevelOption = GetLogLevelOption();

        Option<bool> backingStoreOption = GetBackingStoreOption(defaultConfiguration.UsesBackingStore);

        Option<bool> excludeBackwardCompatible = GetExcludeBackwardCompatibleOption(defaultConfiguration.ExcludeBackwardCompatible);

        Option<bool> additionalDataOption = GetAdditionalDataOption(defaultConfiguration.IncludeAdditionalData);

        Option<List<string>> serializerOption = new(
            "--serializer",
            () => [.. defaultConfiguration.Serializers],
            "The fully qualified class names for serializers. Accepts multiple values. Use `none` to generate a client without any serializer.");
        serializerOption.AddAlias("-s");
        serializerOption.ArgumentHelpName = "classes";

        Option<List<string>> deserializerOption = new(
            "--deserializer",
            () => [.. defaultConfiguration.Deserializers],
            "The fully qualified class names for deserializers. Accepts multiple values. Use `none` to generate a client without any deserializer.");
        deserializerOption.AddAlias("--ds");
        deserializerOption.ArgumentHelpName = "classes";

        Option<bool> cleanOutputOption = GetCleanOutputOption(defaultConfiguration.CleanOutput);

        Option<List<string>> structuredMimeTypesOption = GetStructuredMimeTypesOption([.. defaultConfiguration.StructuredMimeTypes]);

        (Option<List<string>> includePatterns, Option<List<string>> excludePatterns) = GetIncludeAndExcludeOptions(defaultConfiguration.IncludePatterns, defaultConfiguration.ExcludePatterns);

        Option<List<string>> dvrOption = GetDisableValidationRulesOption();

        Option<bool> clearCacheOption = GetClearCacheOption(defaultConfiguration.ClearCache);

        Option<bool> disableSSLValidationOption = GetDisableSSLValidationOption(defaultConfiguration.DisableSSLValidation);

        Command command = new("generate", "Generates a REST HTTP API client from an OpenAPI description file.") {
            descriptionOption,
            manifestOption,
            outputOption,
            classOption,
            typeAccessModifierOption,
            namespaceOption,
            logLevelOption,
            backingStoreOption,
            excludeBackwardCompatible,
            additionalDataOption,
            serializerOption,
            deserializerOption,
            cleanOutputOption,
            structuredMimeTypesOption,
            includePatterns,
            excludePatterns,
            dvrOption,
            clearCacheOption,
            disableSSLValidationOption,
        };
        command.Handler = new ReQuestyGenerateCommandHandler
        {
            DescriptionOption = descriptionOption,
            ManifestOption = manifestOption,
            OutputOption = outputOption,
            ClassOption = classOption,
            TypeAccessModifierOption = typeAccessModifierOption,
            NamespaceOption = namespaceOption,
            LogLevelOption = logLevelOption,
            BackingStoreOption = backingStoreOption,
            ExcludeBackwardCompatibleOption = excludeBackwardCompatible,
            AdditionalDataOption = additionalDataOption,
            SerializerOption = serializerOption,
            DeserializerOption = deserializerOption,
            CleanOutputOption = cleanOutputOption,
            StructuredMimeTypesOption = structuredMimeTypesOption,
            IncludePatternsOption = includePatterns,
            ExcludePatternsOption = excludePatterns,
            DisabledValidationRulesOption = dvrOption,
            ClearCacheOption = clearCacheOption,
            DisableSSLValidationOption = disableSSLValidationOption,
        };
        return command;
    }
    internal static (Option<List<string>>, Option<List<string>>) GetIncludeAndExcludeOptions(HashSet<string> defaultIncludePatterns, HashSet<string> defaultExcludePatterns)
    {
        Option<List<string>> includePatterns = new(
            "--include-path",
            () => defaultIncludePatterns.ToList(),
            "The paths to include in the generation. Glob patterns accepted. Accepts multiple values. Append #OPERATION to the pattern to specify the operation to include. e.g. users/*/messages#GET");
        includePatterns.AddAlias("-i");

        Option<List<string>> excludePatterns = new(
            "--exclude-path",
            () => defaultExcludePatterns.ToList(),
            "The paths to exclude from the generation. Glob patterns accepted. Accepts multiple values. Append #OPERATION to the pattern to specify the operation to exclude. e.g. users/*/messages#GET");
        excludePatterns.AddAlias("-e");
        return (includePatterns, excludePatterns);
    }
    internal static Option<LogLevel> GetLogLevelOption()
    {
#if DEBUG
        static LogLevel DefaultLogLevel() => LogLevel.Debug;
#else
        static LogLevel DefaultLogLevel() => LogLevel.Warning;
#endif
        Option<LogLevel> logLevelOption = new("--log-level", DefaultLogLevel, "The log level to use when logging messages to the main output.");
        logLevelOption.AddAlias("--ll");
        AddEnumValidator(logLevelOption, "log level");
        return logLevelOption;
    }
    private static Option<bool> GetClearCacheOption(bool defaultValue)
    {
        Option<bool> clearCacheOption = new("--clear-cache", () => defaultValue, "Clears any cached data for the current command.");
        clearCacheOption.AddAlias("--cc");
        return clearCacheOption;
    }

    private static Option<bool> GetDisableSSLValidationOption(bool defaultValue)
    {
        Option<bool> disableSSLValidationOption = new("--disable-ssl-validation", () => defaultValue, "Disables SSL certificate validation.");
        disableSSLValidationOption.AddAlias("--dsv");
        return disableSSLValidationOption;
    }

    private static void AddStringRegexValidator(Option<string> option, Regex validator, string parameterName, bool allowEmpty = false)
    {
        option.AddValidator(input =>
        {
            string? value = input.GetValueForOption(option);
            if (string.IsNullOrEmpty(value) && allowEmpty)
            {
                return;
            }

            if (string.IsNullOrEmpty(value) ||
                !validator.IsMatch(value))
            {
                input.ErrorMessage = $"{value} is not a valid {parameterName} for the client, the {parameterName} must conform to {validator}";
            }
        });
    }
    internal static void ValidateAllOrNoneOptions(CommandResult commandResult, params Option[] options)
    {
        IEnumerable<OptionResult?> optionResults = options.Select(option => commandResult.Children.FirstOrDefault(c => c.Symbol == option) as OptionResult);
        List<OptionResult?> optionsWithValue = optionResults.Where(result => result?.Tokens.Any() ?? false).ToList();

        // If not all options are set and at least one is set, it's an error
        if (optionsWithValue.Count > 0 && optionsWithValue.Count < options.Length)
        {
            string[] optionNames = options.Select(option => option.Aliases.FirstOrDefault() ?? "unknown option").ToArray();
            commandResult.ErrorMessage = $"Either all of {string.Join(", ", optionNames)} must be provided or none.";
        }
    }
    internal static void ValidateKnownValues(OptionResult input, string parameterName, IEnumerable<string> knownValues)
    {
        HashSet<string> knownValuesHash = new(knownValues, StringComparer.OrdinalIgnoreCase);
        if (input.Tokens.Any() && input.Tokens.Select(static x => x.Value).SelectMany(static x => x.Split([','], StringSplitOptions.RemoveEmptyEntries)).FirstOrDefault(x => !knownValuesHash.Contains(x)) is string unknownValue)
        {
            string validOptionsList = knownValues.Aggregate(static (x, y) => x + ", " + y);
            input.ErrorMessage = $"{unknownValue} is not a supported {parameterName}, supported values are {validOptionsList}";
        }
    }
    private static void AddEnumValidator<T>(Option<T> option, string parameterName) where T : struct, Enum
    {
        option.AddValidator(input =>
        {
            ValidateKnownValues(input, parameterName, Enum.GetValues<T>().Select(static x => x.ToString()));
        });
    }
    private static void AddEnumValidator<T>(Option<T?> option, string parameterName) where T : struct, Enum
    {
        option.AddValidator(input =>
        {
            ValidateKnownValues(input, parameterName, Enum.GetValues<T>().Select(static x => x.ToString()));
        });
    }
}
