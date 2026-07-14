namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     Reply to a logic-configuration get. <see cref="Config" /> is <c>null</c> when the recipient is already up to
    ///     date; <see cref="SyncStatus" /> always carries the configuration's sync status.
    /// </summary>
    [Schema("LogicConfigurationRetrievedPayload")]
    public record LogicConfigurationRetrievedPayload(LogicConfiguration? Config, ObjectSyncStatus? SyncStatus) : IMessage;
}
