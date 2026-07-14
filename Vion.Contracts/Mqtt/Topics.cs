// ReSharper disable MemberCanBePrivate.Global may be used by other project referencing this shared project

namespace Vion.Contracts.Mqtt
{
    public static class Topics
    {
        #region Mesh - Cloud

        // Mesh -> Cloud
        public const string CloudPropertiesState = "/cloud/sw/properties/state";

        // Mesh -> Cloud
        public const string MeasuringPointsState = "/cloud/sw/measuringPoints/state";

        // Cloud -> Mesh
        public const string PropertiesSubscribe = "/cloud/sw/properties/subscribe";

        // Cloud -> Mesh
        public const string PropertiesUnsubscribe = "/cloud/sw/properties/unsubscribe";

        // Cloud -> Mesh
        public const string PropertySetCloud = "/cloud/sw/property/set";

        // Mesh -> Cloud
        public const string PropertySetResponseCloud = "/cloud/sw/property/set/response";

        // Broker -> Mesh
        public const string SubscriberLastWill = "cloud/subscriber/lastWill";

        // Cloud -> Mesh
        public const string AutomationUpserted = "/cloud/automation/upserted";

        // Cloud -> Mesh
        public const string AutomationDeleted = "/cloud/automation/deleted";

        // Mesh -> Cloud
        public const string AutomationsGet = "/cloud/automations/get";

        // Cloud -> Mesh
        public const string AutomationsRetrieved = "/cloud/automations/retrieved";

        // Mesh -> Cloud
        public const string AlarmRaise = "/cloud/alarm/raise";

        // Mesh -> Cloud
        public const string AlarmResolve = "/cloud/alarm/resolve";

        // Mesh -> Cloud
        public const string AlarmCloseAll = "/cloud/alarm/close/all";

        // Mesh -> Cloud
        public const string SystemEvent = "/cloud/system/event";

        // Cloud -> Mesh
        public const string LogicConfigurationSetCloud = "/cloud/system/logicConfiguration/set";

        // Mesh -> Cloud
        public const string LogicConfigurationSetResponse = "/cloud/system/logicConfiguration/set/response";

        // Mesh -> Cloud
        public const string LogicConfigurationGet = "/cloud/system/logicConfiguration/get";

        // Cloud -> Mesh
        public const string LogicConfigurationRetrieved = "/cloud/system/logicConfiguration/retrieved";

        // Cloud -> Mesh
        public const string RestartCloud = "/cloud/system/restart";

        // Cloud -> Mesh
        public const string LogLevelSet = "/cloud/system/logLevel/set";

        // Mesh -> Cloud
        public const string LogLevelState = "/cloud/system/logLevel/state";

        // Cloud -> Mesh
        // Start / stop a named base-image "system service" (system-control family, beside restart / logLevel). The
        // service name is the FINAL topic segment so it can be authorized per service — compose as
        // SystemServiceStart + "/" + <serviceName> (e.g. .../service/start/remote-console). Mesh dispatches on the
        // composed topic and stays generic (start <serviceName> <arguments>); not an SP property, so invisible on
        // the service surface.
        public const string SystemServiceStart = "/cloud/system/service/start";

        // Cloud -> Mesh
        public const string SystemServiceStop = "/cloud/system/service/stop";

        // Mesh -> Cloud
        // ServiceStateChange events mesh emits whenever a system service starts / stops / fails.
        public const string SystemServiceState = "/cloud/system/service/state";

        // Mesh -> Cloud
        public const string SystemHealthState = "/cloud/system/health/state";

        // Mesh -> Cloud
        public const string SyncStatus = "/cloud/system/syncStatus/state";

        // Mesh -> Cloud
        public const string InstallationTopicGet = "cloud/system/installationTopic/get";

        // Cloud -> Mesh
        public const string InstallationTopicRetrieved = "cloud/system/installationTopic/retrieved";

        // Mesh -> Cloud
        public const string ServiceProviderCloud = "/cloud/system/serviceProvider";

        // Mesh -> Cloud
        public const string ServiceProviderDeclarationCloud = ServiceProviderCloud + "/declaration";

        // Mesh -> Cloud
        public const string ServiceProviderRegistrationCloud = ServiceProviderCloud + "/registration";

        // Cloud -> Mesh
        public const string ServiceProviderRegistrationAcceptedCloud = ServiceProviderRegistrationCloud + "/accepted";

        // Mesh -> Cloud
        public const string ServiceProviderRegistrationAutoAcceptedCloud = ServiceProviderRegistrationCloud + "/autoAccepted";

        // Cloud -> Mesh
        public const string ServiceProviderRegistrationDeniedCloud = ServiceProviderRegistrationCloud + "/denied";

        // Cloud -> Mesh
        public const string ServiceProviderRegistrationDeletedCloud = ServiceProviderRegistrationCloud + "/deleted";

        // Mesh -> Cloud
        public const string ServiceProviderRegistrationsGetCloud = ServiceProviderRegistrationCloud + "s" + "/get";

        // Cloud -> Mesh
        public const string ServiceProviderRegistrationsRetrievedCloud = ServiceProviderRegistrationCloud + "s" + "/retrieved";

        #endregion

        #region Mesh - Dale & Hal

        public const string ComponentHealth = "/component/health";

        // Dale & Hal -> Mesh
        public const string ComponentHealthState = ComponentHealth + "/state";

        // Mesh -> Dale & Hal
        public const string ComponentHealthGet = ComponentHealth + "/get";

        #endregion

        #region Mesh - Dale

        public const string Property = "/sw/property";

        // Mesh -> Dale
        public const string PropertySet = Property + "/set";

        // Dale -> Mesh
        /// <summary>
        ///     Dale never receives messages on this topic. It exists solely to distinguish between synchronous and asynchronous
        ///     property set operations in Mesh.
        ///     In synchronous requests, Mesh waits for a response on the <see cref="PropertySet" /> topic, whereas in asynchronous
        ///     requests,
        ///     Mesh publishes the message and specifies this topic as the response topic without waiting for a reply.
        ///     A separate message handler is responsible for subscribing to this topic.
        /// </summary>
        public const string PropertyAsyncSet = Property + "/async/set";

        // Dale -> Mesh
        public const string PropertyState = Property + "/state";

        public const string MeasuringPoint = "/sw/measuringPoint";

        // Dale -> Mesh
        public const string MeasuringPointState = MeasuringPoint + "/state";

        public const string LogicConfiguration = "/system/logicConfiguration";

        // Mesh -> Dale
        public const string LogicConfigurationSet = LogicConfiguration + "/set";

        #endregion

        #region Hal - Dale

        public const string Di = "/hw/di";

        // Hal -> Dale
        public const string DiState = Di + "/state";

        public const string Do = "/hw/do";

        // Dale -> Hal
        public const string DoSet = Do + "/set";

        // Hal -> Dale
        public const string DoState = Do + "/state";

        public const string Ai = "/hw/ai";

        // Hal -> Dale
        public const string AiState = Ai + "/state";

        public const string Ao = "/hw/ao";

        // Dale -> Hal
        public const string AoSet = Ao + "/set";

        // Hal -> Dale
        public const string AoState = Ao + "/state";

        // Dale -> Hal
        public const string Modbus = "/hw/modbus";

        public const string ModbusSet = Modbus + "/set";

        public const string ModbusGet = Modbus + "/get";

        #endregion

        #region Dale - Dale

        public const string RemoteInterface = "/remote/interface";

        // Dale -> Dale
        public const string RemoteInterfaceSend = RemoteInterface + "/send";

        #endregion

        #region ServiceProvider - Mesh

        // ServiceProvider -> Mesh
        public const string ServiceProvider = "system/serviceProvider"; // No leading slash, as this topic is used as a base for registration topics (no installation topic prefix)

        // ServiceProvider -> Mesh
        public const string ServiceProviderDeclaration = "/" + ServiceProvider + "/declaration";

        // ServiceProvider -> Mesh
        public const string ServiceProviderRegistration = ServiceProvider + "/registration";

        // ServiceProvider -> Mesh
        public const string ServiceProviderRegistrationRequest = ServiceProviderRegistration + "/request";

        // Mesh -> ServiceProvider
        public const string ServiceProviderRegistrationAccepted = ServiceProviderRegistration + "/accepted";

        // Mesh -> ServiceProvider
        public const string ServiceProviderRegistrationDenied = ServiceProviderRegistration + "/denied";

        // Mesh -> ServiceProvider
        public const string ServiceProviderRestart = "/" + ServiceProvider + "/restart";

        // Mesh -> ServiceProvider
        public const string ServiceProviderLogLevelSet = "/" + ServiceProvider + "/logLevel/set";

        // ServiceProvider -> Mesh
        public const string ServiceProviderLogLevelState = "/" + ServiceProvider + "/logLevel/state";

        #endregion
    }
}
