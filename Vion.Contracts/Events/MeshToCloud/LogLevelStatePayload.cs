using Microsoft.Extensions.Logging;

namespace Vion.Contracts.Events.MeshToCloud
{
    [Schema("LogLevelStatePayload")]
    public record LogLevelStatePayload(LogLevel LogLevel) : IMessage;
}