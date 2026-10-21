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

        SystemServiceStarted,

        SystemServiceStopped,

        MeshRestarted,

        BrokerRestarted,

        LogicConfigurationApplied,

        PeersNetworkStateSynced,

        BridgingDeclarationAccepted,

        BridgingDeclarationRejected,

        BridgeApplied,

        BridgeRemoved,
    }

    public enum SystemEventSource
    {
        Unknown,

        Alarm,

        Rule,

        Schedule,

        DiskSpaceMonitor,

        ServiceProvider,

        SystemService,

        SystemControl,

        LogicConfiguration,

        Bridging,
    }

    public enum SystemEventStatus
    {
        Succeeded,

        Failed,

        Unknown,
    }

    public enum SeverityLevel
    {
        Low,

        Medium,

        High,

        Critical,
    }
}
