using System;
using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh.Automations
{
    [Schema("AutomationsRetrievedPayload")]
    public class AutomationsRetrievedPayload : IMessage
    {
        public required List<Automation> Automations { get; set; }

        public Dictionary<Guid, ObjectSyncStatus>? SyncStatusByObjectId { get; set; }
    }
}
