namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     Command that contains the complete configuration for logic blocks, their interfaces, and contract mappings for an
    ///     installation.
    ///     Sent from Cloud to Mesh for forwarding to Dale.
    /// </summary>
    [Schema("SetLogicConfigurationPayload")]
    public class SetLogicConfigurationPayload : LogicConfiguration, IMessage;
}
