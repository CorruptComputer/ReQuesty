using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.RegularExpressions;
using ReQuesty.Handlers;
using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Validation;
using Microsoft.Extensions.Logging;
using System.CommandLine.Hosting;
using ReQuesty.Consts;

namespace ReQuesty;

public static partial class ReQuestyHost
{
    public static RootCommand GetRootCommand()
    {
        RootCommand rootCommand = [];
        rootCommand.Subcommands.Add(GetGenerateCommand());

        return rootCommand;
    }

    private static Command GetGenerateCommand()
    {
        GenerationConfiguration defaultConfiguration = new();
        Option<string> descriptionOption = GetDescriptionOption();
        Option<string> manifestOption = GetManifestOption();

        Option<string> outputOption = GetOutputPathOption(defaultConfiguration.OutputPath);

        Option<string> classOption = new(CommandLineOptions.ClassNameOption, CommandLineOptions.ClassNameShortOption)
        {
            Description = "The class name to use for the core client class.",
            DefaultValueFactory = (_) => defaultConfiguration.ClientClassName,
            HelpName = "name"
        };
        AddStringRegexValidator(classOption, classNameRegex(), "class name");

        Option<AccessModifier> typeAccessModifierOption = GetTypeAccessModifierOption();

        Option<string> namespaceOption = GetNamespaceOption(defaultConfiguration.ClientNamespaceName);

        Option<LogLevel> logLevelOption = GetLogLevelOption();

        Option<bool> backingStoreOption = GetBackingStoreOption(defaultConfiguration.UsesBackingStore);

        Option<bool> excludeBackwardCompatible = GetExcludeBackwardCompatibleOption(defaultConfiguration.ExcludeBackwardCompatible);

        Option<bool> additionalDataOption = GetAdditionalDataOption(defaultConfiguration.IncludeAdditionalData);

        Option<List<string>> serializerOption = new(CommandLineOptions.SerializerOption, CommandLineOptions.SerializerShortOption)
        {
            Description = "The fully qualified class names for serializers. Accepts multiple values. Use `none` to generate a client without any serializer.",
            DefaultValueFactory = (_) => [.. defaultConfiguration.Serializers],
            HelpName = "classes"
        };

        Option<List<string>> deserializerOption = new(CommandLineOptions.DeserializerOption, CommandLineOptions.DeserializerShortOption)
        {
            Description = "The fully qualified class names for deserializers. Accepts multiple values. Use `none` to generate a client without any deserializer.",
            DefaultValueFactory = (_) => [.. defaultConfiguration.Deserializers],
            HelpName = "classes"
        };

        Option<bool> cleanOutputOption = GetCleanOutputOption(defaultConfiguration.CleanOutput);

        Option<List<string>> structuredMimeTypesOption = GetStructuredMimeTypesOption([.. defaultConfiguration.StructuredMimeTypes]);

        (Option<List<string>> includePatterns, Option<List<string>> excludePatterns) = GetIncludeAndExcludeOptions(defaultConfiguration.IncludePatterns, defaultConfiguration.ExcludePatterns);

        Option<List<string>> dvrOption = GetDisableValidationRulesOption();

        Option<bool> clearCacheOption = GetClearCacheOption(defaultConfiguration.ClearCache);

        Option<bool> disableSSLValidationOption = GetDisableSSLValidationOption(defaultConfiguration.DisableSSLValidation);

        Command command = new("generate", "Generates a REST HTTP API client from an OpenAPI description file.")
        {
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

        command.UseCommandHandler<ReQuestyGenerateCommandHandler>();

        return command;
    }

    internal static Option<bool> GetCleanOutputOption(bool defaultValue)
    {
        Option<bool> cleanOutputOption = new(CommandLineOptions.CleanOutputOption, CommandLineOptions.CleanOutputShortOption)
        {
            DefaultValueFactory = (_) => defaultValue,
            Description = "Removes all files from the output directory before generating the code files."
        };

        return cleanOutputOption;
    }

    internal static Option<string> GetOutputPathOption(string defaultValue)
    {
        Option<string> outputOption = new(CommandLineOptions.OutputOption, CommandLineOptions.OutputShortOption)
        {
            DefaultValueFactory = (_) => defaultValue,
            Description = "The output directory path for the generated code files.",
            HelpName = "path"
        };

        return outputOption;
    }

    internal static Option<List<string>> GetDisableValidationRulesOption()
    {
        string[] validationRules =
        [
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

        Option<List<string>> option = new(CommandLineOptions.DisableValidationRulesOption, CommandLineOptions.DisableValidationRulesShortOption)
        {
            DefaultValueFactory = (_) => [],
            Description = "The OpenAPI description validation rules to disable. Accepts multiple values.",
            HelpName = string.Join(",", validationRules),
            Arity = ArgumentArity.ZeroOrMore,
        };

        option.Validators.Add(x => ValidateKnownValues(x, CommandLineOptions.DisableValidationRulesOption, validationRules));

        return option;
    }

    internal static Option<string> GetDescriptionOption(bool required = false)
    {
        Option<string> descriptionOption = new(CommandLineOptions.DescriptionOption, CommandLineOptions.DescriptionShortOption)
        {
            Description = "The path or URI to the OpenAPI description file used to generate the code files.",
            HelpName = "path",
            Required = required
        };

        return descriptionOption;
    }

    private static Option<string> GetManifestOption()
    {
        Option<string> manifestOption = new(CommandLineOptions.ManifestOption, CommandLineOptions.ManifestShortOption)
        {
            Description = "The path or URI to the API manifest file used to generate the code files. Append #apikey if the target manifest contains multiple API dependencies entries."
        };

        return manifestOption;
    }

    [GeneratedRegex(@"^[a-zA-Z_][\w]*", RegexOptions.Singleline, 500)]
    private static partial Regex classNameRegex();

    [GeneratedRegex(@"^[\w][\w\._-]+", RegexOptions.Singleline, 500)]
    private static partial Regex namespaceNameRegex();

    internal static Option<AccessModifier> GetTypeAccessModifierOption()
    {
        Option<AccessModifier> accessOption = new("--type-access-modifier", "--tam")
        {
            Description = "The type access modifier to use for the client types.",
            DefaultValueFactory = (_) => AccessModifier.Public
        };

        AddEnumValidator(accessOption, "type-access-modifier");
        return accessOption;
    }

    internal static Option<string> GetNamespaceOption(string defaultNamespaceName)
    {
        Option<string> namespaceOption = new("--namespace-name", "-n")
        {
            Description = "The namespace to use for the core client class specified with the --class-name option.",
            DefaultValueFactory = (_) => defaultNamespaceName,
            HelpName = "name"
        };

        AddStringRegexValidator(namespaceOption, namespaceNameRegex(), "namespace name", string.IsNullOrEmpty(defaultNamespaceName));
        return namespaceOption;
    }

    internal static Option<bool> GetBackingStoreOption(bool defaultValue = false)
    {
        Option<bool> backingStoreOption = new("--backing-store", "-b")
        {
            Description = "Enables backing store for models.",
            DefaultValueFactory = (_) => defaultValue
        };

        return backingStoreOption;
    }

    internal static Option<bool> GetExcludeBackwardCompatibleOption(bool defaultValue = false)
    {
        Option<bool> excludeBackwardCompatible = new("--exclude-backward-compatible", "--ebc")
        {
            Description = "Excludes backward compatible and obsolete assets from the generated result. Should be used for new clients.",
            DefaultValueFactory = (_) => defaultValue
        };

        return excludeBackwardCompatible;
    }

    internal static Option<bool> GetAdditionalDataOption(bool defaultValue = true)
    {
        Option<bool> additionalDataOption = new("--additional-data", "--ad")
        {
            Description = "Includes the 'AdditionalData' property for models.",
            DefaultValueFactory = (_) => defaultValue
        };

        return additionalDataOption;
    }

    internal static Option<List<string>> GetStructuredMimeTypesOption(List<string> defaultValue)
    {
        Option<List<string>> structuredMimeTypesOption = new("--structured-mime-types", "-m")
        {
            Description = "The MIME types with optional priorities as defined in RFC9110 Accept header to use for structured data model generation. Accepts multiple values.",
            DefaultValueFactory = (_) => defaultValue
        };

        return structuredMimeTypesOption;
    }

    internal static (Option<List<string>>, Option<List<string>>) GetIncludeAndExcludeOptions(HashSet<string> defaultIncludePatterns, HashSet<string> defaultExcludePatterns)
    {
        Option<List<string>> includePatterns = new("--include-path", "-i")
        {
            Description = "The paths to include in the generation. Glob patterns accepted. Accepts multiple values. Append #OPERATION to the pattern to specify the operation to include. e.g. users/*/messages#GET",
            DefaultValueFactory = (_) => [.. defaultIncludePatterns]
        };

        Option<List<string>> excludePatterns = new("--exclude-path", "-e")
        {
            Description = "The paths to exclude from the generation. Glob patterns accepted. Accepts multiple values. Append #OPERATION to the pattern to specify the operation to exclude. e.g. users/*/messages#GET",
            DefaultValueFactory = (_) => [.. defaultExcludePatterns]
        };

        return (includePatterns, excludePatterns);
    }

    internal static Option<LogLevel> GetLogLevelOption()
    {
        LogLevel defaultLogLevel
#if DEBUG
            = LogLevel.Debug;
#else
            = LogLevel.Warning;
#endif

        Option<LogLevel> logLevelOption = new("--log-level", "--ll")
        {
            Description = "The log level to use when logging messages to the main output.",
            DefaultValueFactory = (_) => defaultLogLevel
        };

        AddEnumValidator(logLevelOption, "log level");
        return logLevelOption;
    }

    private static Option<bool> GetClearCacheOption(bool defaultValue)
    {
        Option<bool> clearCacheOption = new("--clear-cache", "--cc")
        {
            Description = "Clears any cached data for the current command.",
            DefaultValueFactory = (_) => defaultValue
        };

        return clearCacheOption;
    }

    private static Option<bool> GetDisableSSLValidationOption(bool defaultValue)
    {
        Option<bool> disableSSLValidationOption = new("--disable-ssl-validation", "--dsv")
        {
            Description = "Disables SSL certificate validation.",
            DefaultValueFactory = (_) => defaultValue
        };

        return disableSSLValidationOption;
    }

    private static void AddStringRegexValidator(Option<string> option, Regex validator, string parameterName, bool allowEmpty = false)
    {
        option.Validators.Add(input =>
        {
            string? value = input.GetValue(option);
            if (string.IsNullOrEmpty(value) && allowEmpty)
            {
                return;
            }

            if (string.IsNullOrEmpty(value) ||
                !validator.IsMatch(value))
            {
                input.AddError($"{value} is not a valid {parameterName} for the client, the {parameterName} must conform to {validator}");
            }
        });
    }

    internal static void ValidateKnownValues(OptionResult input, string parameterName, IEnumerable<string> knownValues)
    {
        HashSet<string> knownValuesHash = new(knownValues, StringComparer.OrdinalIgnoreCase);
        if (input.Tokens.Any() && input.Tokens.Select(static x => x.Value).SelectMany(static x => x.Split([','], StringSplitOptions.RemoveEmptyEntries)).FirstOrDefault(x => !knownValuesHash.Contains(x)) is string unknownValue)
        {
            string validOptionsList = knownValues.Aggregate(static (x, y) => x + ", " + y);
            input.AddError($"{unknownValue} is not a supported {parameterName}, supported values are {validOptionsList}");
        }
    }

    private static void AddEnumValidator<T>(Option<T> option, string parameterName) where T : struct, Enum
    {
        option.Validators.Add(input =>
        {
            ValidateKnownValues(input, parameterName, Enum.GetValues<T>().Select(static x => x.ToString()));
        });
    }
}
