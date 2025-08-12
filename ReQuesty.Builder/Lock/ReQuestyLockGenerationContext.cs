using System.Text.Json.Serialization;

namespace ReQuesty.Builder.Lock;

[JsonSerializable(typeof(ReQuestyLock))]
internal partial class ReQuestyLockGenerationContext : JsonSerializerContext
{
}
