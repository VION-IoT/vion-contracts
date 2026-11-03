using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Vion.Contracts.Events.CloudToMesh;
using Vion.Contracts.Events.CloudToMesh.Automations;
using Vion.Contracts.Events.MeshToCloud;
using Vion.Contracts.Events.ServiceProviderToMesh;

// ReSharper disable PartialTypeWithSinglePart this must be a partial class because it is generated

namespace Vion.Contracts.Events
{
    /// <summary>
    ///     Source-generation context for every top-level <c>Events/*</c> payload: the one wire definition of the JSON the
    ///     platform's mesh bus carries. <c>events.schema.json</c> beside this file is exported from these options, see
    ///     <see cref="EventsSchemaExport" />.
    ///     <para>
    ///         The options are the ones every mesh context serializes these payloads with: camelCase property names and
    ///         dictionary keys, and every enum a payload carries written as its member name through the generic
    ///         <see cref="JsonStringEnumConverter{TEnum}" />. The non-generic converter is <c>[RequiresDynamicCode]</c>
    ///         and the generator rejects it with <c>SYSLIB1034</c>.
    ///     </para>
    ///     <para>
    ///         Enums are listed here rather than pinned with a <see cref="JsonConverterAttribute" /> on the enum type, which
    ///         is where <c>HwJsonContext</c> pins its own. These enums have been serialized for years by consumers with
    ///         their own options; an attribute on the type would change the wire form for every reflection-based consumer
    ///         of that enum, not only for callers of this context.
    ///     </para>
    ///     <para>
    ///         Two pairs of payloads share a type name across directions —
    ///         <c>ServiceProviderRegistrationAcceptedPayload</c> and <c>ServiceProviderRegistrationDeniedPayload</c>, in
    ///         <c>CloudToMesh</c> and <c>MeshToServiceProvider</c> — so each carries a <c>TypeInfoPropertyName</c>
    ///         prefixed with its direction.
    ///     </para>
    /// </summary>

    // CloudToMesh
    [JsonSerializable(typeof(AutomationsRetrievedPayload))]
    [JsonSerializable(typeof(UpsertAutomationPayload))]
    [JsonSerializable(typeof(InstallationTopicRetrievedPayload))]
    [JsonSerializable(typeof(PeersNetworkStatePayload))]
    [JsonSerializable(typeof(RestartPayload))]
    [JsonSerializable(typeof(ServiceProviderRegistrationAcceptedPayload), TypeInfoPropertyName = "CloudToMeshServiceProviderRegistrationAcceptedPayload")]
    [JsonSerializable(typeof(ServiceProviderRegistrationDeletedPayload))]
    [JsonSerializable(typeof(ServiceProviderRegistrationDeniedPayload), TypeInfoPropertyName = "CloudToMeshServiceProviderRegistrationDeniedPayload")]
    [JsonSerializable(typeof(ServiceProviderRegistrationsRetrievedPayload))]
    [JsonSerializable(typeof(SetLogLevelPayload))]
    [JsonSerializable(typeof(SetLogicConfigurationPayload))]
    [JsonSerializable(typeof(SetPropertyPayload))]
    [JsonSerializable(typeof(StartSystemServicePayload))]
    [JsonSerializable(typeof(StopSystemServicePayload))]
    [JsonSerializable(typeof(SubscribePropertiesPayload))]
    [JsonSerializable(typeof(UnsubscribePropertiesPayload))]

    // MeshToCloud
    [JsonSerializable(typeof(AlarmCloseAllPayload))]
    [JsonSerializable(typeof(AlarmStateChangedPayload))]
    [JsonSerializable(typeof(CertificateStatePayload))]
    [JsonSerializable(typeof(GetAutomationsPayload))]
    [JsonSerializable(typeof(GetInstallationTopicPayload))]
    [JsonSerializable(typeof(GetLogicConfigurationPayload))]
    [JsonSerializable(typeof(GetPeersNetworkStatePayload))]
    [JsonSerializable(typeof(GetServiceProviderRegistrationsPayload))]
    [JsonSerializable(typeof(LogLevelStatePayload))]
    [JsonSerializable(typeof(MeasuringPointsStatePayload))]
    [JsonSerializable(typeof(NetworkStatePayload))]
    [JsonSerializable(typeof(PropertiesStatePayload))]
    [JsonSerializable(typeof(ServiceProviderDeclarationPayload))]
    [JsonSerializable(typeof(ServiceProviderRegistrationPayload))]
    [JsonSerializable(typeof(SetPropertyResponsePayload))]
    [JsonSerializable(typeof(SyncStatusUpdatedEventPayload))]
    [JsonSerializable(typeof(SystemEventPayload))]
    [JsonSerializable(typeof(SystemHealthStatusPayload))]
    [JsonSerializable(typeof(SystemServiceStatePayload))]

    // MeshToServiceProvider
    [JsonSerializable(typeof(MeshToServiceProvider.ServiceProviderRegistrationAcceptedPayload),
                      TypeInfoPropertyName = "MeshToServiceProviderServiceProviderRegistrationAcceptedPayload")]
    [JsonSerializable(typeof(MeshToServiceProvider.ServiceProviderRegistrationDeniedPayload),
                      TypeInfoPropertyName = "MeshToServiceProviderServiceProviderRegistrationDeniedPayload")]

    // ServiceProviderToMesh
    [JsonSerializable(typeof(BridgingDeclarationPayload))]
    [JsonSerializable(typeof(ComponentHealthStatusPayload))]
    [JsonSerializable(typeof(LogicConfigurationLoadFailedPayload))]
    [JsonSerializable(typeof(MeasuringPointStatePayload))]
    [JsonSerializable(typeof(PropertySetResponsePayload))]
    [JsonSerializable(typeof(PropertyStatePayload))]
    [JsonSerializable(typeof(ServiceProviderRegistrationRequestPayload))]

    // AlarmStateChangedPayload.AlarmParameter.Value and the ServiceProviderDeclarationPayload annotations are typed
    // object, and a source-generated context can only write an object whose runtime type it has metadata for. These are
    // the runtime types mesh's AlarmJsonContext registers for the alarm parameters, plus JsonElement — what an object
    // value is once read — so a payload this context has read can be written again.
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(DateTime))]
    [JsonSerializable(typeof(TimeSpan))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                                 DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
                                 Converters =
                                 [
                                     // CloudToMesh
                                     typeof(JsonStringEnumConverter<ActionType>),
                                     typeof(JsonStringEnumConverter<AutomationType>),
                                     typeof(JsonStringEnumConverter<ParameterType>),
                                     typeof(JsonStringEnumConverter<RegistrationStatus>),
                                     typeof(JsonStringEnumConverter<ScheduleType>),
                                     typeof(JsonStringEnumConverter<TimingType>),
                                     typeof(JsonStringEnumConverter<TimingUnit>),
                                     typeof(JsonStringEnumConverter<TriggerType>),

                                     // MeshToCloud
                                     typeof(JsonStringEnumConverter<ConnectionStatus>),
                                     typeof(JsonStringEnumConverter<HealthStatus>),
                                     typeof(JsonStringEnumConverter<ServiceState>),
                                     typeof(JsonStringEnumConverter<SeverityLevel>),
                                     typeof(JsonStringEnumConverter<SyncStatus>),
                                     typeof(JsonStringEnumConverter<SystemEventSource>),
                                     typeof(JsonStringEnumConverter<SystemEventStatus>),
                                     typeof(JsonStringEnumConverter<SystemEventType>),

                                     // ServiceProviderToMesh
                                     typeof(JsonStringEnumConverter<AccessType>),
                                     typeof(JsonStringEnumConverter<LogicConfigurationLoadFailureReason>),

                                     // Microsoft.Extensions.Logging
                                     typeof(JsonStringEnumConverter<LogLevel>),
                                 ])]
    public partial class EventsJsonContext : JsonSerializerContext;
}
