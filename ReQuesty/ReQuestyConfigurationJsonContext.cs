
using System.Text.Json.Serialization;
using ReQuesty.Builder.Configuration;

namespace ReQuesty;

[JsonSerializable(typeof(ReQuestyConfiguration))]
internal partial class ReQuestyConfigurationJsonContext : JsonSerializerContext
{

}
