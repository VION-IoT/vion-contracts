using System.Text.Json.Nodes;

namespace Vion.Contracts.Events.MeshToCloud
{
    public readonly record struct SetPropertyResponsePayload(string ServiceProviderIdentifier, string ServiceIdentifier, string PropertyIdentifier, JsonNode? Value);
}
