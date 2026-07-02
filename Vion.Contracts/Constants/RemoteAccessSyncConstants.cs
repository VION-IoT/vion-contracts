namespace Vion.Contracts.Constants
{
    /// <summary>
    ///     Shared vocabulary for the remote-access use of the sync-status convergence channel
    ///     (<see cref="Vion.Contracts.Events.MeshToCloud.SyncStatusUpdatedEventPayload" />). Cloud-api tags a
    ///     remote-access session's sync-status with <see cref="ObjectType" />; the gateway reports convergence detail
    ///     under the well-known <see cref="FeedbackKeys" /> so cloud-api reads it back without magic strings.
    /// </summary>
    public static class RemoteAccessSyncConstants
    {
        /// <summary>
        ///     Sync-status object-type / use-case tag for a remote-access session. Cloud-api sets this as the
        ///     <c>SyncStatusEntity.ObjectType</c>; it must equal <c>RemoteAccessSessionEntity.AggregateType</c>.
        /// </summary>
        public const string ObjectType = "RemoteAccessSession";

        /// <summary>
        ///     Well-known keys the gateway populates in <c>SyncStatusUpdatedEventPayload.Feedback</c> as a session
        ///     converges, and cloud-api reads back onto the session record.
        /// </summary>
        public static class FeedbackKeys
        {
            /// <summary>The gateway's address on the session tailnet (e.g. <c>100.x.y.z</c>).</summary>
            public const string TunnelAddress = "tunnelAddress";

            /// <summary>The tunnel network interface the session brought up.</summary>
            public const string TunnelInterface = "tunnelInterface";

            /// <summary>The gateway's node identifier within the session tailnet.</summary>
            public const string TailnetNodeId = "tailnetNodeId";

            /// <summary>Whether host sshd is listening on the tunnel interface (console profile only).</summary>
            public const string SshdListening = "sshdListening";

            /// <summary>Human-readable failure detail when a session fails to converge.</summary>
            public const string Error = "error";
        }
    }
}
