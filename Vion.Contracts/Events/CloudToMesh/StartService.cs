using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     Starts a named base-image service on the gateway (system-control family, beside restart / logLevel). The
    ///     service is the FINAL segment of the topic — compose as
    ///     <see cref="Vion.Contracts.Mqtt.Topics.ServiceStart" /> + "/" + &lt;serviceName&gt; (e.g.
    ///     <c>.../service/start/remoteConsole</c>) — so it is authorized per service and stays invisible on the
    ///     service-provider surface. Parameters travel as named <see cref="ServiceArgument" />s. See the architecture
    ///     spec <c>2026-06-30-on-demand-remote-gateway-access</c>.
    /// </summary>
    [Schema("StartService")]
    public record StartService(List<ServiceArgument> Arguments) : IMessage;
}
