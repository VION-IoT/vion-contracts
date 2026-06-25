using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh
{
    [Schema("SubscribePropertiesPayload")]
    public record SubscribePropertiesPayload(List<Subscription> Subscriptions) : IMessage;

    public record Subscription(string ServiceProviderIdentifier, string ServiceIdentifier, List<string> PropertyIdentifiers);
}
