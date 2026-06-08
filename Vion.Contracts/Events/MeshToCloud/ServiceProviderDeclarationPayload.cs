using System.Collections.Generic;
using System.Text.Json.Nodes;

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

        /// <summary>
        ///     A property exposed by a declared service. Its data shape, UI hints, and runtime behaviour
        ///     travel as three sibling documents — the same model dale introspection uses
        ///     (<c>Introspection.LogicBlockIntrospectionResult.ServicePropertyInfo</c>) and cloud-api
        ///     persists verbatim.
        /// </summary>
        public class PropertyInfo
        {
            /// <summary>The identifier of the property, e.g. "Port", "IsEnabled".</summary>
            public required string Identifier { get; set; }

            /// <summary>
            ///     JSON Schema 2020-12 document (Dale profile) describing the property's data shape —
            ///     type, format, nullability, <c>readOnly</c> (non-writable), <c>writeOnly</c> (redacted),
            ///     unit and bounds. Required: every declared property carries a schema, so a malformed
            ///     declaration is rejected at deserialisation rather than persisted schema-less.
            /// </summary>
            public required JsonNode Schema { get; set; }

            /// <summary>Optional UI presentation hints (display name, group, ordering, ...). May be null.</summary>
            public JsonNode? Presentation { get; set; }

            /// <summary>Optional runtime behaviour hints (e.g. <c>persistent</c>). May be null.</summary>
            public JsonNode? Runtime { get; set; }
        }

        /// <summary>
        ///     A measuring point exposed by a declared service. Same sibling-document model as
        ///     <see cref="PropertyInfo" /> (mirrors <c>ServiceMeasuringPointInfo</c> on the dale side).
        /// </summary>
        public class MeasuringPointInfo
        {
            /// <summary>The identifier of the measuring point, e.g. "Throughput".</summary>
            public required string Identifier { get; set; }

            /// <summary>
            ///     JSON Schema 2020-12 document (Dale profile) describing the measuring point's data shape.
            ///     Required: every declared measuring point carries a schema (see <see cref="PropertyInfo.Schema" />).
            /// </summary>
            public required JsonNode Schema { get; set; }

            /// <summary>Optional UI presentation hints. May be null.</summary>
            public JsonNode? Presentation { get; set; }

            /// <summary>Optional runtime behaviour hints. May be null.</summary>
            public JsonNode? Runtime { get; set; }
        }

        public class ContractInfo
        {
            public required string Identifier { get; set; }

            public required string Type { get; set; }
        }
    }
}
