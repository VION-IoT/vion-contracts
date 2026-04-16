namespace Vion.Contracts.Events.MeshToCloud
{
    /// <summary>
    ///     Event sent from Mesh to Cloud to indicate the result of a SetLogicConfiguration.
    /// </summary>
    [Schema("SetLogicConfigurationResponsePayload")]
    public class SetLogicConfigurationResponsePayload : IMessage
    {
        public required bool Success { get; set; }

        public string? ErrorMessage { get; set; }
    }
}