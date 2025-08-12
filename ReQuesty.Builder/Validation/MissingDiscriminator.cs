using System.Collections.Concurrent;
using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Extensions;
using Microsoft.OpenApi;

namespace ReQuesty.Builder.Validation;

public class MissingDiscriminator(GenerationConfiguration configuration) : ValidationRule<OpenApiDocument>(nameof(MissingDiscriminator), (context, document) =>
    {
        ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> idx = new(StringComparer.OrdinalIgnoreCase);
        document.InitializeInheritanceIndex(idx);
        if (document.Components is { Schemas.Count: > 0 })
        {
            Parallel.ForEach(document.Components.Schemas, entry =>
            {
                ValidateSchema(entry.Value, context, idx, entry.Key);
            });
        }

        (string Key, IOpenApiSchema Schema)[] inlineSchemasToValidate = document.Paths
                                        ?.Where(static x => x.Value.Operations is not null)
                                        .SelectMany(static x => x.Value.Operations!.Values.Select(y => (x.Key, Operation: y)))
                                        .SelectMany(x => x.Operation.GetResponseSchemas(OpenApiOperationExtensions.SuccessCodes, configuration.StructuredMimeTypes).Select(y => (x.Key, Schema: y)))
                                        .Where(static x => x.Schema is OpenApiSchema)
                                        .ToArray() ?? [];
        Parallel.ForEach(inlineSchemasToValidate, entry =>
        {
            ValidateSchema(entry.Schema, context, idx, entry.Key);
        });
    })
{
    private static void ValidateSchema(IOpenApiSchema schema, IValidationContext context, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> idx, string address)
    {
        if (!schema.IsInclusiveUnion() && !schema.IsExclusiveUnion())
        {
            return;
        }

        if ((schema.AnyOf is null || schema.AnyOf.All(static x => !x.IsObjectType())) &&
            (schema.OneOf is null || schema.OneOf.All(static x => !x.IsObjectType())))
        {
            return;
        }

        if (string.IsNullOrEmpty(schema.GetDiscriminatorPropertyName()) || !schema.GetDiscriminatorMappings(idx).Any())
        {
            context.CreateWarning(nameof(MissingDiscriminator), $"The schema {address} is a polymorphic type but does not define a discriminator. This will result in serialization errors.");
        }
    }
}
