using System;
using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh
{
    [Schema("ServiceProviderRegistrationsRetrievedPayload")]
    public record ServiceProviderRegistrationsRetrievedPayload(List<ServiceProviderRegistration> Registrations, Dictionary<Guid, ObjectSyncStatus>? SyncStatusByObjectId)
        : IMessage;

    public record ServiceProviderRegistration(string Identifier, string Secret, RegistrationStatus Status);

    public enum RegistrationStatus
    {
        Pending,

        Denied,

        Accepted,
    }
}
