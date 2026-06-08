namespace Vion.Contracts.Events.MeshToCloud
{
    [Schema("ServiceProviderRegistrationPayload")]
    public record ServiceProviderRegistrationPayload(string Secret) : IMessage;
}
