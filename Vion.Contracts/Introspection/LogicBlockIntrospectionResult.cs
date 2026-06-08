using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Vion.Contracts.Introspection
{
    /// <summary>
    ///     Describes the introspected structure of a LogicBlock, including its interfaces, contracts, and services.
    ///     This is the shared contract between the dale parser (producer) and cloud-api (consumer).
    /// </summary>
    public class LogicBlockIntrospectionResult
    {
        /// <summary>
        ///     e.g. "Ecocoach.LogicBlocks.EnergyConsumer.ChargingStationX"
        /// </summary>
        public string TypeFullName { get; set; } = null!;

        /// <summary>
        ///     The function interfaces implemented/contained in the logic block.
        ///     A binding may carry a consumer-side link-multiplicity annotation
        ///     (see <see cref="InterfaceInfo.Annotations" />).
        /// </summary>
        public List<InterfaceInfo> Interfaces { get; set; } = [];

        /// <summary>
        ///     The contracts (service provider mappable endpoints) of the logic block.
        /// </summary>
        public List<ContractInfo> Contracts { get; set; } = [];

        /// <summary>
        ///     Gets the collection of services associated with the logic block.
        /// </summary>
        public List<ServiceInfo> Services { get; set; } = [];

        /// <summary>
        ///     Block-level annotations from LogicBlockInfoAttribute (DefaultName, Icon, etc.)
        /// </summary>
        public Dictionary<string, object> Annotations { get; set; } = [];

        public class InterfaceInfo
        {
            /// <summary>
            ///     The identifier of the function interface, e.g. "chargingPoint2"
            /// </summary>
            public string Identifier { get; set; } = null!;

            /// <summary>
            ///     e.g. Dale.Core.LogicBlocks.Interfaces.EnergyConsumerControl.IConsumer for a charging point
            /// </summary>
            public List<string> InterfaceTypeFullNames { get; set; } = [];

            /// <summary>
            ///     e.g. Dale.Core.LogicBlocks.Interfaces.EnergyConsumerControl.IController for related energy
            ///     manager/distribution
            /// </summary>
            public List<string> MatchingInterfaceTypeFullNames { get; set; } = [];

            /// <summary>
            ///     UI annotations. Common keys: DefaultName, Tags, ContractName, ArrowDirection, RoleDefaultName,
            ///     MatchingRoleDefaultName. A non-default consumer-side link multiplicity additionally adds the
            ///     <see cref="Vion.Contracts.Conventions.LogicBlockWiringConventions.MultiplicityAnnotationKey" />
            ///     key, valued with one of the
            ///     <see cref="Vion.Contracts.Conventions.LogicBlockWiringConventions" /> token strings.
            /// </summary>
            public Dictionary<string, object> Annotations { get; set; } = [];
        }

        public class ContractInfo
        {
            /// <summary>
            ///     The identifier of the contract, e.g. "light_do"
            /// </summary>
            public string Identifier { get; set; } = null!;

            /// <summary>
            ///     e.g. Dale.Core.LogicBlocks.Io.IDigitalInput
            /// </summary>
            public string ContractTypeFullName { get; set; } = null!;

            /// <summary>
            ///     Service provider contract type
            ///     e.g. DigitalInput, DigitalOutput, TemperatureInput, AnalogOutput20mA, ModbusRtu
            /// </summary>
            public string MatchingContractType { get; set; } = null!;

            /// <summary>
            ///     UI annotations. Common key: DefaultName. A non-default consumer-side link multiplicity adds the
            ///     <see cref="Vion.Contracts.Conventions.LogicBlockWiringConventions.MultiplicityAnnotationKey" />
            ///     key; a provider-side consumer cap adds the
            ///     <see cref="Vion.Contracts.Conventions.LogicBlockWiringConventions.ConsumersAnnotationKey" />
            ///     key — each valued with one of the
            ///     <see cref="Vion.Contracts.Conventions.LogicBlockWiringConventions" /> token strings.
            /// </summary>
            public Dictionary<string, object> Annotations { get; set; } = [];
        }

        public class ServiceInfo
        {
            /// <summary>
            ///     The identifier of the Service, e.g. "default", "circuit1"
            /// </summary>
            public string Identifier { get; set; } = null!;

            /// <summary>
            ///     Implemented interface types.
            /// </summary>
            public List<string> InterfaceTypeFullNames { get; set; } = [];

            /// <summary>
            ///     Service properties exposed by this service
            /// </summary>
            public List<ServicePropertyInfo> Properties { get; set; } = [];

            /// <summary>
            ///     Service measuring points exposed by this service
            /// </summary>
            public List<ServiceMeasuringPointInfo> MeasuringPoints { get; set; } = [];

            /// <summary>
            ///     Inward relations (from other services/interfaces to this service)
            /// </summary>
            public List<ServiceRelationInfo> InwardRelations { get; set; } = [];

            /// <summary>
            ///     Outward relations (from this service to other services/interfaces)
            /// </summary>
            public List<ServiceRelationInfo> OutwardRelations { get; set; } = [];
        }

        public class ServicePropertyInfo
        {
            /// <summary>
            ///     The identifier of the property, e.g. "MaxCurrent", "IsActive".
            /// </summary>
            public string Identifier { get; set; } = null!;

            /// <summary>
            ///     JSON Schema 2020-12 document (Dale profile) describing the property's data shape.
            ///     Required: every introspected property carries a schema.
            /// </summary>
            public required JsonNode Schema { get; set; }

            /// <summary>
            ///     Optional UI presentation hints (display name, group, ordering, etc.). May be omitted (null)
            ///     for properties without presentation metadata.
            /// </summary>
            public JsonNode? Presentation { get; set; }

            /// <summary>
            ///     Optional dale-runtime behavior hints (e.g. <c>persistent</c>). May be omitted (null) for
            ///     properties without runtime metadata.
            /// </summary>
            public JsonNode? Runtime { get; set; }
        }

        public class ServiceMeasuringPointInfo
        {
            /// <summary>
            ///     The identifier of the measuring point, e.g. "Power", "VoltageL1".
            /// </summary>
            public string Identifier { get; set; } = null!;

            /// <summary>
            ///     JSON Schema 2020-12 document (Dale profile) describing the measuring point's data shape.
            ///     Required: every introspected measuring point carries a schema.
            /// </summary>
            public required JsonNode Schema { get; set; }

            /// <summary>
            ///     Optional UI presentation hints. May be omitted (null).
            /// </summary>
            public JsonNode? Presentation { get; set; }

            /// <summary>
            ///     Optional dale-runtime behavior hints. May be omitted (null).
            /// </summary>
            public JsonNode? Runtime { get; set; }
        }

        public class ServiceRelationInfo
        {
            /// <summary>
            ///     The relation type identifier, e.g. "LightToToggle"
            /// </summary>
            public string RelationType { get; set; } = null!;

            /// <summary>
            ///     The identifier of the related interface, e.g. "light"
            /// </summary>
            public string InterfaceIdentifier { get; set; } = null!;

            /// <summary>
            ///     The full name of the related interface type, e.g. "Dale.Core.LogicBlocks.Interfaces.Toggling.IToggleable"
            /// </summary>
            public string InterfaceTypeFullName { get; set; } = null!;

            /// <summary>
            ///     UI information like DefaultName
            /// </summary>
            public Dictionary<string, object> Annotations { get; set; } = [];
        }
    }
}
