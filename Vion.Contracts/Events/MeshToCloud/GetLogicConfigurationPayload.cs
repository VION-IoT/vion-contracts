namespace Vion.Contracts.Events.MeshToCloud
{
    /// <summary>
    ///     Logic-configuration get body; <see cref="AppliedHash" /> is the last-applied content hash, or <c>null</c> if none.
    /// </summary>
    [Schema("GetLogicConfigurationPayload")]
    public record GetLogicConfigurationPayload(string? AppliedHash) : IMessage;
}
