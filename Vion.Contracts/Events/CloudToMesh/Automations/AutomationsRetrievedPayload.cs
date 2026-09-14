using System;
using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh.Automations
{
    [Schema("AutomationsRetrievedPayload")]
    public record AutomationsRetrievedPayload(List<Automation> Automations, Dictionary<Guid, ObjectSyncStatus> SyncStatusByObjectId) : IMessage;
}
