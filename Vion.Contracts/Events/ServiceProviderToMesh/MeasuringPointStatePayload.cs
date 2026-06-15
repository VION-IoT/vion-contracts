using System.Text.Json.Nodes;

namespace Vion.Contracts.Events.ServiceProviderToMesh
{
    /// <summary>
    ///     Reports the current value of a single measuring point from a service provider to Mesh.
    /// </summary>
    /// <param name="Value">The measuring-point value, or <c>null</c>.</param>
    [Schema("MeasuringPointStatePayload")]
    public record MeasuringPointStatePayload(JsonNode? Value) : IMessage;
}
