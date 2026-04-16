using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh
{
    [Schema("UnsubscribePropertiesPayload")]
    public record UnsubscribePropertiesPayload(Dictionary<string, Dictionary<string, List<string>>> ProviderServiceProperties) : IMessage;
}