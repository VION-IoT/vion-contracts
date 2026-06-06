namespace Vion.Contracts.Events.MeshToServiceProvider
{
    public readonly record struct ServiceProviderRegistrationAcceptedPayload(
        string InstallationTopic,
        string Host,
        int Port,
        string ClientId,
        string Username,
        string Password);
}
