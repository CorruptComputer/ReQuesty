using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Extensions;
using Microsoft.OpenApi;

namespace ReQuesty.Builder.Validation;

public class UrlFormEncodedComplex : ValidationRule<OpenApiOperation>
{
    private static readonly StructuredMimeTypesCollection validContentTypes = [
        "application/x-www-form-urlencoded",
    ];
    public UrlFormEncodedComplex() : base(nameof(UrlFormEncodedComplex), static (context, operation) =>
    {
        if (operation.GetRequestSchema(validContentTypes) is { } requestSchema)
        {
            ValidateSchema(requestSchema, context, "request body");
        }

        if (operation.GetResponseSchema(validContentTypes) is { } responseSchema)
        {
            ValidateSchema(responseSchema, context, "response body");
        }
    })
    {
    }
    private static void ValidateSchema(IOpenApiSchema schema, IValidationContext context, string schemaName)
    {
        if (schema == null)
        {
            return;
        }

        if (!schema.IsObjectType())
        {
            context.CreateWarning(nameof(UrlFormEncodedComplex), $"The operation {context.PathString} has a {schemaName} which is not an object type. This is not supported by ReQuesty and serialization will fail.");
        }

        if (schema.Properties is not null && schema.Properties.Any(static x => x.Value.IsObjectType()))
        {
            context.CreateWarning(nameof(UrlFormEncodedComplex), $"The operation {context.PathString} has a {schemaName} with a complex properties and the url form encoded content type. This is not supported by ReQuesty and serialization of complex properties will fail.");
        }
    }
}
