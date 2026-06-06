using Microsoft.Extensions.Logging;

namespace Vion.Contracts.Events.CloudToMesh
{
    [Schema("SetLogLevelPayload")]
    public record SetLogLevelPayload(LogLevel LogLevel) : IMessage;
}
