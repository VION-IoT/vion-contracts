using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh
{
    [Schema("SubscribePropertiesPayload")]
    public record SubscribePropertiesPayload(Dictionary<string, Dictionary<string, List<string>>> ProviderServiceProperties) : IMessage;
}
