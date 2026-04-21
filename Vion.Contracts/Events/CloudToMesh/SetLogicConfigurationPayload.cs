using System;
using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     Command that contains the complete configuration for logic blocks, their interfaces, and contract mappings for an
    ///     installation.
    ///     Sent from Cloud to Mesh for forwarding to Dale
    /// </summary>
    [Schema("SetLogicConfigurationPayload")]
    public class SetLogicConfigurationPayload : IMessage
    {
        public required List<LogicBlockInstance> LogicBlockInstances { get; set; }

        public required List<InterfaceMapping> InterfaceMappings { get; set; }

        public required List<ContractMapping> ContractMappings { get; set; }

        /// <summary>
        ///     Pre-authorized HTTPS URLs for each distinct (PackageId, PackageVersion) referenced by LogicBlockInstances.
        ///     Consumed by Dale's plugin loader to download packages without needing storage credentials of its own.
        /// </summary>
        public List<LogicBlockLibrarySource> LogicBlockLibrarySources { get; set; } = [];

        /// <summary>
        ///     Deprecated. Use ContractMappings instead. Will be removed in next major version.
        /// </summary>
        [Obsolete("Use ContractMappings instead.")]
        public List<IoMapping> IoMappings { get; set; } = [];

        /// <summary>
        ///     Deprecated. Use ContractMappings instead. Will be removed in next major version.
        /// </summary>
        [Obsolete("Use ContractMappings instead.")]
        public List<HardwareBlock> HardwareBlocks { get; set; } = [];

        public class LogicBlockInstance
        {
            public required string Id { get; set; }

            public required string PackageId { get; set; }

            public required string PackageVersion { get; set; }

            public required string TypeFullName { get; set; }

            public required string Name { get; set; }

            public required List<ServiceIdMapping> Services { get; set; }
        }

        public class ServiceIdMapping
        {
            public required string Identifier { get; set; }

            public required string ServiceId { get; set; }
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

        /// <summary>
        ///     Deprecated. Use ContractMapping instead.
        /// </summary>
        [Obsolete("Use ContractMapping instead.")]
        public class IoMapping
        {
            public required string LogicBlockInstanceId { get; set; }

            public required string IoIdentifier { get; set; }

            public required string InstallationTopic { get; set; }

            public required string MappedHardwareBlockInstanceId { get; set; }

            public required string MappedHardwareElementIdentifier { get; set; }

            public required string MappedInstallationTopic { get; set; }
        }

        /// <summary>
        ///     Deprecated. Use ContractMapping instead.
        /// </summary>
        [Obsolete("Use ContractMapping instead.")]
        public class HardwareBlock
        {
            public required string Id { get; set; }
        }
    }
}