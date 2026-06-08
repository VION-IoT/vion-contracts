using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh
{
    [Schema("ServiceProviderRegistrationsRetrievedPayload")]
    public record ServiceProviderRegistrationsRetrievedPayload(List<ServiceProviderRegistration> Registrations) : IMessage;

    public record ServiceProviderRegistration(string Identifier, string Secret, RegistrationStatus Status);

    public enum RegistrationStatus
    {
        Pending,

        Denied,

        Accepted,
    }
}
