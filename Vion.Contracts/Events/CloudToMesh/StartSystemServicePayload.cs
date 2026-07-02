using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     Starts a named base-image system service on the gateway (system-control family, beside restart /
    ///     logLevel). The service is the FINAL segment of the topic — compose as
    ///     <see cref="Vion.Contracts.Mqtt.Topics.SystemServiceStart" /> + "/" + &lt;serviceName&gt; (e.g.
    ///     <c>.../service/start/remote-console</c>) — so it is authorized per service and stays invisible on the
    ///     service-provider surface. Mesh stays generic: it just runs the named service with these arguments.
    ///     Parameters travel as a list of named <see cref="ServiceArgument" />s (names in
    ///     <see cref="Vion.Contracts.Constants.RemoteAccessConstants.Arguments" />). See the architecture spec
    ///     <c>2026-06-30-on-demand-remote-gateway-access</c>.
    /// </summary>
    [Schema("StartSystemService")]
    public record StartSystemServicePayload(List<ServiceArgument> Arguments) : IMessage;
}
