using System.CommandLine;
using ReQuesty.Builder.Configuration;

namespace ReQuesty.Consts;

public static class CommandLineOptions
{

    public const string AdditionalDataOption = "--additional-data";
    public const string AdditionalDataShortOption = "--ad";

    public const string BackingStoreOption = "--backing-store";
    public const string BackingStoreShortOption = "-b";

    public const string ClassNameOption = "--class-name";
    public const string ClassNameShortOption = "-c";

    public const string CleanOutputOption = "--clean-output";
    public const string CleanOutputShortOption = "--co";

    public const string ClearCacheOption = "--clear-cache";
    public const string ClearCacheShortOption = "--cc";

    public const string DescriptionOption = "--openapi";
    public const string DescriptionShortOption = "-d";

    public const string DeserializerOption = "--deserializer";
    public const string DeserializerShortOption = "--ds";

    public const string DisableSSLValidationOption = "--disable-ssl-validation";
    public const string DisableSSLValidationShortOption = "--dsv";

    public const string DisableValidationRulesOption = "--disable-validation-rules";
    public const string DisableValidationRulesShortOption = "--dvr";

    public const string ExcludeBackwardCompatibleOption = "--exclude-backward-compatible";
    public const string ExcludeBackwardCompatibleShortOption = "--ebc";

    public const string ExcludePathOption = "--exclude-path";
    public const string ExcludePathShortOption = "-e";

    public const string IncludePathOption = "--include-path";
    public const string IncludePathShortOption = "-i";

    public const string LogLevelOption = "--log-level";
    public const string LogLevelShortOption = "--ll";

    public const string ManifestOption = "--manifest";
    public const string ManifestShortOption = "-a";

    public const string NamespaceNameOption = "--namespace-name";
    public const string NamespaceNameShortOption = "-n";

    public const string OutputOption = "--output";
    public const string OutputShortOption = "-o";

    public const string SerializerOption = "--serializer";
    public const string SerializerShortOption = "-s";

    public const string StructuredMimeTypesOption = "--structured-mime-types";
    public const string StructuredMimeTypesShortOption = "-m";

    public const string TypeAccessModifierOption = "--type-access-modifier";
    public const string TypeAccessModifierShortOption = "--tam";
}
