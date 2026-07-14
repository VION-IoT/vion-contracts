using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     The complete configuration for logic blocks, their interfaces, and contract mappings for an installation.
    ///     Shared body of <see cref="SetLogicConfigurationPayload" /> and <see cref="LogicConfigurationRetrievedPayload" />.
    /// </summary>
    public class LogicConfiguration
    {
        public required List<LogicBlockInstance> LogicBlockInstances { get; set; }

        public required List<InterfaceMapping> InterfaceMappings { get; set; }

        public required List<ContractMapping> ContractMappings { get; set; }

        /// <summary>
        ///     Pre-authorized HTTPS URLs for each distinct (PackageId, PackageVersion) referenced by LogicBlockInstances.
        ///     Consumed by Dale's plugin loader to download packages without needing storage credentials of its own.
        /// </summary>
        public List<LogicBlockLibrarySource> LogicBlockLibrarySources { get; set; } = [];

        public class LogicBlockInstance
        {
            public required string Id { get; set; }

            public required string PackageId { get; set; }

            public required string PackageVersion { get; set; }

            public required string TypeFullName { get; set; }

            public required string Name { get; set; }

            public required List<ServiceIdMapping> Services { get; set; }

            /// <summary>
            ///     Operator-chosen <c>[InstantiationParameter]</c> values (RFC 0016 / config-time structural
            ///     gating), applied to the block before <c>Configure</c> so inclusion gates resolve at bind time.
            ///     The first config-time value channel on an instance. Additive and nullable: <c>null</c> (or an
            ///     empty list) for instances with no parameters and for payloads produced before the feature
            ///     shipped.
            ///     <para>
            ///         A <b>list, never an identifier-keyed dictionary</b> (decision 0045): the platform's JSON
            ///         serialization applies a camelCase <c>DictionaryKeyPolicy</c>, so a dictionary
            ///         <c>{ "ChargePointCount": 3 }</c> would arrive as <c>{ "chargePointCount": 3 }</c> and no
            ///         longer match the case-sensitive introspection identifier. Identifiers therefore travel as
            ///         <see cref="InstantiationParameterValue.Identifier" /> <em>values</em>, where no key policy
            ///         touches them.
            ///     </para>
            /// </summary>
            public List<InstantiationParameterValue>? InstantiationParameterValues { get; set; }
        }

        public class ServiceIdMapping
        {
            public required string Identifier { get; set; }

            public required string ServiceId { get; set; }
        }

        /// <summary>
        ///     One operator-chosen <c>[InstantiationParameter]</c> value on a
        ///     <see cref="LogicBlockInstance" /> (RFC 0016). <see cref="Identifier" /> is the case-sensitive
        ///     introspection property identifier; <see cref="Value" /> is the JSON scalar (enum as a member-name
        ///     string, integer as a number, bool, or string) applied to the block before <c>Configure</c>.
        /// </summary>
        public class InstantiationParameterValue
        {
            public required string Identifier { get; set; }

            public JsonNode? Value { get; set; }
        }

        public class InterfaceMapping
        {
            public required string LogicBlockInstanceId { get; set; }

            public required string InterfaceIdentifier { get; set; }

            public required string InstallationTopic { get; set; }

            public required string MappedLogicBlockInstanceId { get; set; }

            public required string MappedInterfaceIdentifier { get; set; }

            public required string MappedInstallationTopic { get; set; }
        }

        public class ContractMapping
        {
            public required string LogicBlockInstanceId { get; set; }

            public required string ContractIdentifier { get; set; }

            public required string InstallationTopic { get; set; }

            public required string MappedServiceProviderIdentifier { get; set; }

            public required string MappedServiceIdentifier { get; set; }

            public required string MappedContractIdentifier { get; set; }

            public required string MappedInstallationTopic { get; set; }
        }

        public class LogicBlockLibrarySource
        {
            public required string PackageId { get; set; }

            public required string PackageVersion { get; set; }

            public required string DownloadUrl { get; set; }

            public required DateTimeOffset DownloadUrlExpiresAt { get; set; }
        }
    }
}
