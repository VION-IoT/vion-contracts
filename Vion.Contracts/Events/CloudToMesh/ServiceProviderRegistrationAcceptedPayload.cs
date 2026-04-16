namespace Vion.Contracts.Events.CloudToMesh
{
    [Schema("ServiceProviderRegistrationAcceptedPayload")]
    public record ServiceProviderRegistrationAcceptedPayload(string Secret) : IMessage;
}