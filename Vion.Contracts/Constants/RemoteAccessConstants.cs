namespace Vion.Contracts.Constants
{
    /// <summary>
    ///     Shared vocabulary for on-demand remote access, which rides the generic
    ///     <see cref="Vion.Contracts.Events.CloudToMesh.StartService" /> /
    ///     <see cref="Vion.Contracts.Events.CloudToMesh.StopService" />
    ///     commands. <see cref="Services" /> are the final topic segment (authorized per service);
    ///     <see cref="Arguments" /> are the argument names carried in the command; <see cref="Sync" /> is the
    ///     remote-access use of the sync-status convergence channel. Keeps these strings off the wire as literals in
    ///     both cloud-api and mesh.
    /// </summary>
    public static class RemoteAccessConstants
    {
        /// <summary>
        ///     Service names — the final segment of <see cref="Vion.Contracts.Mqtt.Topics.ServiceStart" /> /
        ///     <see cref="Vion.Contracts.Mqtt.Topics.ServiceStop" />, and the per-service authorization gate
        ///     (RemoteVpnAccess / RemoteConsoleAccess).
        /// </summary>
        public static class Services
        {
            public const string RemoteVpn = "remoteVpn";

            public const string RemoteConsole = "remoteConsole";
        }

        /// <summary>
        ///     Well-known <see cref="Vion.Contracts.Events.CloudToMesh.ServiceArgument" /> names for the remote-access
        ///     StartService command (StopService needs only <see cref="SessionId" />).
        /// </summary>
        public static class Arguments
        {
            /// <summary>The RemoteAccessSession id; also names the gateway's per-session unit instance.</summary>
            public const string SessionId = "sessionId";

            /// <summary>The session's dedicated Headscale control-server URL (HTTPS).</summary>
            public const string LoginServerUrl = "loginServerUrl";

            /// <summary>Single-use, TTL-bound pre-auth key for the session tailnet.</summary>
            public const string EphemeralAuthKey = "ephemeralAuthKey";

            /// <summary>Hard expiry (ISO 8601); the gateway tears the session down at this time (fail closed).</summary>
            public const string ExpiresAtUtc = "expiresAtUtc";
        }

        /// <summary>
        ///     The remote-access use of the sync-status convergence channel
        ///     (<see cref="Vion.Contracts.Events.MeshToCloud.SyncStatusUpdatedEventPayload" />). Cloud-api tags a
        ///     session's sync-status with <see cref="ObjectType" /> and reads convergence detail back from the
        ///     well-known <see cref="FeedbackKeys" /> the gateway emits.
        /// </summary>
        public static class Sync
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

                /// <summary>Whether host sshd is listening on the tunnel interface (console service only).</summary>
                public const string SshdListening = "sshdListening";

                /// <summary>Human-readable failure detail when a session fails to converge.</summary>
                public const string Error = "error";
            }
        }
    }
}
