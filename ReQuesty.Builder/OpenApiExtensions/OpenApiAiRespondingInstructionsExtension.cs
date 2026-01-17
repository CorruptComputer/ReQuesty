using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace ReQuesty.Builder.OpenApiExtensions;

public class OpenApiAiRespondingInstructionsExtension : IOpenApiExtension
{
    public static string Name => "x-ai-responding-instructions";

    public List<string> RespondingInstructions { get; init; } = [];

    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (RespondingInstructions != null &&
            RespondingInstructions.Count != 0)
        {
            writer.WriteStartArray();
            foreach (string instruction in RespondingInstructions)
            {
                writer.WriteValue(instruction);
            }
            writer.WriteEndArray();
        }
    }

    public static OpenApiAiRespondingInstructionsExtension Parse(JsonNode source)
    {
        if (source is not JsonArray rawArray)
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        OpenApiAiRespondingInstructionsExtension result = new();
        result.RespondingInstructions.AddRange(rawArray.OfType<JsonValue>().Where(static x => x.GetValueKind() is JsonValueKind.String).Select(static x => x.GetValue<string>()));
        return result;
    }
}
