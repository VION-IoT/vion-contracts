namespace Vion.Contracts.Events.CloudToMesh
{
    [Schema("ServiceProviderRegistrationDeniedPayload")]
    public record ServiceProviderRegistrationDeniedPayload(string Secret, string? Reason) : IMessage;
}