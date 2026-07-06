namespace Vion.Contracts.Constants
{
    /// <summary>
    ///     Shared vocabulary for on-demand remote access, which rides the generic
    ///     <see cref="Vion.Contracts.Events.CloudToMesh.StartSystemServicePayload" /> /
    ///     <see cref="Vion.Contracts.Events.CloudToMesh.StopSystemServicePayload" /> commands.
    ///     <see cref="Services" /> are the final topic segment (authorized per service, and the wrapper-service names
    ///     mesh invokes); <see cref="Arguments" /> are the argument names carried in the command. Keeps these strings
    ///     off the wire as literals in both cloud-api and mesh.
    /// </summary>
    public static class RemoteAccessConstants
    {
        /// <summary>
        ///     Service names — the final segment of <see cref="Vion.Contracts.Mqtt.Topics.SystemServiceStart" /> /
        ///     <see cref="Vion.Contracts.Mqtt.Topics.SystemServiceStop" />, and the per-service authorization gate
        ///     (RemoteVpnAccess / RemoteConsoleAccess). These are also the wrapper-service names mesh invokes on the
        ///     gateway — mesh stays generic, running <c>start &lt;serviceName&gt; &lt;arguments&gt;</c> without knowing
        ///     the service — so they MUST match (kebab-case, Linux service convention) the wrapper service names in
        ///     the hardware-integration base image.
        /// </summary>
        public static class Services
        {
            public const string RemoteVpn = "remote-vpn";

            public const string RemoteConsole = "remote-console";
        }

        /// <summary>
        ///     Well-known <see cref="Vion.Contracts.Events.CloudToMesh.ServiceArgument" /> names carried in the
        ///     <c>Arguments</c> of the remote-access
        ///     <see cref="Vion.Contracts.Events.CloudToMesh.StartSystemServicePayload" /> command. The session id is
        ///     NOT an argument — it is the command's required top-level <c>InstanceId</c> field, and it is all
        ///     StopSystemService needs.
        /// </summary>
        public static class Arguments
        {
            /// <summary>The session's dedicated Headscale control-server URL (HTTPS).</summary>
            public const string LoginServerUrl = "loginServerUrl";

            /// <summary>Single-use, TTL-bound pre-auth key for the session tailnet.</summary>
            public const string EphemeralAuthKey = "ephemeralAuthKey";

            /// <summary>Hard expiry (ISO 8601); the gateway tears the session down at this time (fail closed).</summary>
            public const string ExpiresAtUtc = "expiresAtUtc";
        }
    }
}
