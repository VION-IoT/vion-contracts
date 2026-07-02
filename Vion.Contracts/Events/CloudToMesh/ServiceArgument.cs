namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     A single named argument for a <see cref="StartService" /> / <see cref="StopService" /> command. Argument
    ///     names are a per-service contract; the remote-access ones are in
    ///     <see cref="Vion.Contracts.Constants.RemoteAccessConstants.Arguments" />.
    /// </summary>
    public record ServiceArgument(string Name, string Value);
}
