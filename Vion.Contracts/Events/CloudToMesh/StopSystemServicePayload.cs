using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     Stops a named base-image system service on the gateway (system-control family). The service is the FINAL
    ///     segment of the topic — compose as <see cref="Vion.Contracts.Mqtt.Topics.SystemServiceStop" /> + "/" +
    ///     &lt;serviceName&gt; (e.g. <c>.../service/stop/remote-console</c>). The <see cref="ServiceArgument" />s
    ///     identify the instance to stop (e.g. the session id — see
    ///     <see cref="Vion.Contracts.Constants.RemoteAccessConstants.Arguments" />). See the architecture spec
    ///     <c>2026-06-30-on-demand-remote-gateway-access</c>.
    /// </summary>
    [Schema("StopSystemServicePayload")]
    public record StopSystemServicePayload(List<ServiceArgument> Arguments) : IMessage;
}
