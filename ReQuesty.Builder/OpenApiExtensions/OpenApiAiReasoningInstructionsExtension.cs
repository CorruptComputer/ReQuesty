using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace ReQuesty.Builder.OpenApiExtensions;

public class OpenApiAiReasoningInstructionsExtension : IOpenApiExtension
{
    public static string Name => "x-ai-reasoning-instructions";

    public List<string> ReasoningInstructions { get; init; } = [];

    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (ReasoningInstructions != null &&
            ReasoningInstructions.Count != 0)
        {
            writer.WriteStartArray();
            foreach (string instruction in ReasoningInstructions)
            {
                writer.WriteValue(instruction);
            }
            writer.WriteEndArray();
        }
    }

    public static OpenApiAiReasoningInstructionsExtension Parse(JsonNode source)
    {
        if (source is not JsonArray rawArray)
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        OpenApiAiReasoningInstructionsExtension result = new();
        result.ReasoningInstructions.AddRange(rawArray.OfType<JsonValue>().Where(static x => x.GetValueKind() is JsonValueKind.String).Select(static x => x.GetValue<string>()));
        return result;
    }
}
