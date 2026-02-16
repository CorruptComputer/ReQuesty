using Microsoft.OpenApi;

namespace ReQuesty.Builder.Validation;

public class InconsistentTypeFormatPair : ValidationRule<IOpenApiSchema>
{
    private static readonly Dictionary<JsonSchemaType, HashSet<string>> validPairs = new()
    {
        [JsonSchemaType.String] = new(StringComparer.OrdinalIgnoreCase) {
            "commonmark",
            "html",
            "date",
            "date-time",
            "duration",
            "time",
            "base64url",
            "uuid",
            "binary",
            "byte",
        },
        [JsonSchemaType.Integer] = new(StringComparer.OrdinalIgnoreCase) {
            "int32",
            "int64",
            "int8",
            "uint8",
            "int16",
            "uint16",
        },
        [JsonSchemaType.Number] = new(StringComparer.OrdinalIgnoreCase) {
            "float",
            "double",
            "decimal",
            "int32",
            "int64",
            "int8",
            "uint8",
            "int16",
            "uint16",
        },
    };
    private static readonly HashSet<JsonSchemaType> escapedTypes = [
        JsonSchemaType.Array,
        JsonSchemaType.Boolean,
        JsonSchemaType.Null,
        JsonSchemaType.Object,
    ];
    public InconsistentTypeFormatPair() : base(nameof(InconsistentTypeFormatPair), static (context, schema) =>
    {
        if (schema is null || !schema.Type.HasValue || string.IsNullOrEmpty(schema.Format) || KnownAndNotSupportedFormats.knownAndUnsupportedFormats.Contains(schema.Format) || escapedTypes.Contains(schema.Type.Value))
        {
            return;
        }

        JsonSchemaType sanitizedType = schema.Type.Value & ~JsonSchemaType.Null;

        // Some things are generated as "Integer | String", but the String is only there as a fallback.
        // Will fuck up the lookup below so remove it.
        if (sanitizedType.HasFlag(JsonSchemaType.String)
            && (sanitizedType & ~JsonSchemaType.String) != 0
            && (sanitizedType & ~JsonSchemaType.String) != JsonSchemaType.Null)
        {
            sanitizedType &= ~JsonSchemaType.String;
        }

        if (!validPairs.TryGetValue(sanitizedType, out HashSet<string>? validFormats) || !validFormats.Contains(schema.Format))
        {
            context.CreateWarning(nameof(InconsistentTypeFormatPair), $"The format {schema.Format} is not supported by ReQuesty for the type {sanitizedType} and the string type will be used.");
        }
    })
    {
    }
}
