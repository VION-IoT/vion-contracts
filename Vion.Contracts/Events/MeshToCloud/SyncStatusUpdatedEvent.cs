using System;
using System.Collections.Generic;

namespace Vion.Contracts.Events.MeshToCloud
{
    [Schema("SyncStatusUpdatedEventPayload")]
    public class SyncStatusUpdatedEventPayload : IMessage // device -> cloud
    {
        public Guid Id { get; set; }

        public SyncStatus Status { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public Dictionary<string, string>? Feedback { get; set; }
    }

    public enum SyncStatus
    {
        Pending = 0,

        InProgress,

        Completed,

        Failed,
    }
}
