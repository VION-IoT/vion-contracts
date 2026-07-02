using System;
using Vion.Contracts.Events.MeshToCloud;

namespace Vion.Contracts.Events.CloudToMesh
{
    public readonly record struct ObjectSyncStatus(Guid SyncStatusId, SyncStatus Status);
}
