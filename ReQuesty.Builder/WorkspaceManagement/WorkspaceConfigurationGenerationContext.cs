
using System.Text.Json.Serialization;

namespace ReQuesty.Builder.WorkspaceManagement;

[JsonSerializable(typeof(WorkspaceConfiguration))]
[JsonSerializable(typeof(ApiClientConfiguration))]
internal partial class WorkspaceConfigurationGenerationContext : JsonSerializerContext
{
}
