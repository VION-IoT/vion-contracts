using System.Collections.Generic;

namespace Vion.Contracts.Events.MeshToCloud
{
    /// <summary>
    ///     Mesh -&gt; Cloud. Emitted whenever a base-image system service (remote-access, etc.) changes state on
    ///     <see cref="Vion.Contracts.Mqtt.Topics.SystemServiceState" />. Carries an optional
    ///     <see cref="Information" /> dictionary for additional detail — e.g. connection information for the client
    ///     when a service comes up. This is the dedicated service-state channel; the sync-status channel is NOT
    ///     reused for service state. Mesh additionally emits a <see cref="SystemEventPayload" />
    ///     (<see cref="SystemEventType.SystemServiceStarted" /> / <see cref="SystemEventType.SystemServiceStopped" />)
    ///     for the audit trail.
    /// </summary>
    [Schema("SystemServiceStatePayload")]
    public record ServiceStatePayload(string ServiceName, string InstanceId, ServiceState State, Dictionary<string, string>? Information = null) : IMessage;

    public enum ServiceState
    {
        Started,

        Stopped,

        Failed,
    }
}
