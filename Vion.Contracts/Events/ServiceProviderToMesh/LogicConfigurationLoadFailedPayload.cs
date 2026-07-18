namespace Vion.Contracts.Events.ServiceProviderToMesh
{
    /// <summary>
    ///     Reports to Mesh that a service provider could not load its persisted logic configuration.
    /// </summary>
    /// <param name="Reason">Why the load failed.</param>
    [Schema("LogicConfigurationLoadFailedPayload")]
    public record LogicConfigurationLoadFailedPayload(LogicConfigurationLoadFailureReason Reason) : IMessage;

    /// <summary>
    ///     Why a logic configuration failed to load.
    /// </summary>
    public enum LogicConfigurationLoadFailureReason
    {
        /// <summary>
        ///     The persisted configuration file is present but could not be parsed.
        /// </summary>
        CorruptConfig,

        /// <summary>
        ///     No persisted configuration file exists, so the service provider has no configuration to run.
        /// </summary>
        MissingConfig,
    }
}
