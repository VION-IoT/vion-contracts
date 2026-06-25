using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh
{
    [Schema("UnsubscribePropertiesPayload")]
    public record UnsubscribePropertiesPayload(List<Subscription> Subscriptions) : IMessage;
}