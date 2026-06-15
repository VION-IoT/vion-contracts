using System.Text.Json.Nodes;

namespace Vion.Contracts.Events.ServiceProviderToMesh
{
    /// <summary>
    ///     The service provider's reply to a property-set request, published on the request's <c>ResponseTopic</c>.
    /// </summary>
    /// <param name="Value">The applied property value, or <c>null</c>.</param>
    [Schema("PropertySetResponsePayload")]
    public record PropertySetResponsePayload(JsonNode? Value) : IMessage;
}
