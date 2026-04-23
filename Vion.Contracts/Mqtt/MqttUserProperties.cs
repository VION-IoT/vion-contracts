namespace Vion.Contracts.Mqtt
{
    public static class MqttUserProperties
    {
        public static class Schema
        {
            public const string Name = "schema";
        }

        public static class Status
        {
            public const string Name = "status";

            public const string Success = "success";

            public const string Error = "error";
        }

        public static class ErrorCode
        {
            public const string Name = "error_code";
        }

        public static class ErrorMessage
        {
            public const string Name = "error_message";
        }

        public static class PublishedAt
        {
            public const string Name = "published_at";

            public const string Format = "yyyy-MM-ddTHH:mm:ss.fffffffZ"; // todo why not just use "o" or "O" ? See test in HeaderSerializerShould
        }
        
        public static class CreatedBy
        {
            public const string Name = "created_by";
        }

        public static class TraceParent
        {
            public const string Name = "traceparent";
        }

        public static class SyncStatus
        {
            public const string Name = "sync_status_id";
        }
    }
}