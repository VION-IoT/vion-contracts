namespace Vion.Contracts.Constants
{
    public class SystemHealthReasonConstants
    {
        public const string ConnectionStatusNotReceived = "Connection status not yet received";

        public const string HealthCheckNotExecutedReason = "Health check has not yet been executed";

        public const string HealthCheckTimeoutReason = "Health check response not received within expected time";

        public const string MeshOffline = "Mesh is offline";

        public const string RemovedFromMonitoring = "Removed from monitoring";
    }
}