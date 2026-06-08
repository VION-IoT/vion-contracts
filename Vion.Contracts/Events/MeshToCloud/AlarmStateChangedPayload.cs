using System;

namespace Vion.Contracts.Events.MeshToCloud
{
    [Schema("AlarmStateChangedPayload")]
    public class AlarmStateChangedPayload : IMessage
    {
        public required Guid AutomationId { get; set; }

        public required string Identifier { get; set; }

        public AlarmParameter[]? AlarmParameters { get; set; }

        public readonly record struct AlarmParameter(string Name, object Value);
    }
}
