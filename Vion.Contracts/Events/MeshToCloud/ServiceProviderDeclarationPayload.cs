using System.Collections.Generic;

namespace Vion.Contracts.Events.MeshToCloud
{
    [Schema("ServiceProviderDeclarationPayload")]
    public class ServiceProviderDeclarationPayload : IMessage
    {
        public required List<ServiceInfo> Services { get; set; }

        public Dictionary<string, object>? Annotations { get; set; }

        public class ServiceInfo
        {
            public required string Identifier { get; set; }

            public string? Description { get; set; }

            public Dictionary<string, object>? Annotations { get; set; }

            public List<PropertyInfo>? Properties { get; set; }

            public List<MeasuringPointInfo>? MeasuringPoints { get; set; }

            public List<ContractInfo>? Contracts { get; set; }
        }

        public class PropertyInfo
        {
            public required string Identifier { get; set; }

            public bool Writable { get; set; }

            public required string Type { get; set; }

            public Dictionary<string, object>? Annotations { get; set; }
        }

        public class MeasuringPointInfo
        {
            public required string Identifier { get; set; }

            public required string Type { get; set; }

            public Dictionary<string, object>? Annotations { get; set; }
        }

        public class ContractInfo
        {
            public required string Identifier { get; set; }

            public required string Type { get; set; }
        }
    }
}
