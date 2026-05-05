using System.Text.Json.Nodes;

namespace Vion.Contracts.Events.CloudToMesh
{
    [Schema("SetPropertyPayload")]
    public record SetPropertyPayload(JsonNode? Value, JsonNode Schema) : IMessage;
}
