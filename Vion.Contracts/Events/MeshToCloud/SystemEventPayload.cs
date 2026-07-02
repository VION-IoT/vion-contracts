using System;

namespace Vion.Contracts.Events.MeshToCloud
{
    [Schema("SystemEventPayload")]
    public class SystemEventPayload : IMessage
    {
        public string? ObjectId { get; set; }

        public Guid? CorrelationId { get; set; }

        public SystemEventType SystemEventType { get; set; }

        public SystemEventSource SystemEventSource { get; set; }

        public SystemEventStatus Status { get; set; }

        public SeverityLevel Level { get; set; }

        public string? Data { get; set; }

        public string? Message { get; set; }

        public DateTime DateTime { get; set; }
    }

    public enum SystemEventType
    {
        Unknown,

        SetProperty,

        GetProperty,

        GetMeasuringPoint,

        RaiseAlarm,

        ResolveAlarm,

        AutomationUpserted,

        AutomationDeleted,

        AutomationsSynced,

        AutomationsInitialized,

        Monitoring,

        ServiceProviderRegistered,

        ServiceProviderAccepted,

        ServiceProviderDenied,

        ServiceProviderDeleted,

        ServiceProvidersSynced,

        ServiceProviderRestarted,

        // Audit trail for mesh starting / stopping a base-image system service
        // (remote-access, etc.). Appended (never reordered) — the enum is wire.
        SystemServiceStarted,

        SystemServiceStopped,
    }

    public enum SystemEventSource
    {
        Unknown,

        Alarm,

        Rule,

        Schedule,

        DiskSpaceMonitorService,

        ServiceProvider,

        SystemService,
    }

    public enum SystemEventStatus
    {
        Succeeded,

        Failed,
    }

    public enum SeverityLevel
    {
        Low,

        Medium,

        High,

        Critical,
    }
}
