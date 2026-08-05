using System;
using System.Collections.Generic;

namespace Vion.Contracts.Events.MeshToCloud
{
    /// <summary>
    ///     Represents the health status of all edge device components as reported by Mesh to the cloud.
    /// </summary>
    /// <param name="Components">
    ///     The list of components being monitored e.g. Mesh, Dale, Hal, FlashMq, Otel-Collector.
    /// </param>
    [Schema("SystemHealthStatusPayload")]
    public record SystemHealthStatusPayload(List<Component> Components) : IMessage;

    /// <summary>
    ///     Represents the status of a single component or subcomponent.
    /// </summary>
    /// <param name="Name">
    ///     The name of the component (e.g., "mesh", "dale", "hal", "flashmq", "otel-collector") or subcomponent (e.g.,
    ///     "DiskSpaceMonitor", "AutomationService").
    /// </param>
    /// <param name="ConnectionStatus">
    ///     The connection status of the component. The meaning varies by component type, for example:
    ///     <list type="bullet">
    ///         <item>For Mesh, Dale, and Hal: whether they are connected to the MQTT broker.</item>
    ///         <item>For FlashMq: whether the broker is reachable.</item>
    ///         <item>For internal Mesh services: whether the service is running.</item>
    ///     </list>
    ///     When a component is <see cref="MeshToCloud.ConnectionStatus.Offline" />, its <see cref="HealthStatus" /> becomes
    ///     <see cref="MeshToCloud.HealthStatus.Unknown" />
    ///     because the component could still be functioning correctly but unable to communicate (e.g., Dale is executing
    ///     correctly but FlashMq is down).
    /// </param>
    /// <param name="HealthStatus">
    ///     The health status of the component. This is <see cref="MeshToCloud.HealthStatus.Unknown" /> when:
    ///     <list type="bullet">
    ///         <item>The component is offline and its actual health cannot be determined.</item>
    ///         <item>Mesh has not yet performed a health check on the component.</item>
    ///         <item>The component just connected and its health state is not yet known.</item>
    ///     </list>
    /// </param>
    /// <param name="Since">
    ///     The timestamp when the component entered its current state. This is <c>null</c> when:
    ///     <list type="bullet">
    ///         <item>The component ungracefully disconnected and the offline state is only known through a last will message.</item>
    ///         <item>The timestamp is not applicable.</item>
    ///     </list>
    ///     When a component remains in the same state (e.g., unhealthy for hours), this timestamp reflects when it first
    ///     entered that state, not when it was last checked.
    /// </param>
    /// <param name="Reason">
    ///     An optional reason explaining why the component is in its current health state.
    ///     Examples: "Disk usage above 95%", "Did not receive response within x amount of time", "Connection refused".
    /// </param>
    /// <param name="SubComponents">
    ///     Optional list of subcomponents or internal services. For example, Mesh may have subcomponents like
    ///     DiskSpaceMonitor, AutomationService, or MessagePublisherService that can individually be healthy or
    ///     unhealthy.
    ///     If any subcomponent is unhealthy, the parent component should also be marked as unhealthy.
    /// </param>
    public readonly record struct Component(
        string Name,
        ConnectionStatus ConnectionStatus,
        HealthStatus HealthStatus,
        DateTime? Since,
        string? Reason = null,
        List<Component>? SubComponents = null);

    public enum ConnectionStatus
    {
        Unknown,

        Offline,

        Online,
    }

    public enum HealthStatus
    {
        Unknown,

        Unhealthy,

        Healthy,
    }
}
