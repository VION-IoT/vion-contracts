using System.Text.Json.Nodes;

namespace Vion.Contracts.Events.ServiceProviderToMesh
{
    /// <summary>
    ///     Reports the current value of a property from a service provider to Mesh.
    /// </summary>
    /// <param name="Value">The property value, or <c>null</c>.</param>
    [Schema("PropertyStatePayload")]
    public record PropertyStatePayload(JsonNode? Value) : IMessage;
}
