namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     A single named argument for a <see cref="StartServicePayload" /> / <see cref="StopServicePayload" />
    ///     command. Arguments are a list of these readonly records rather than a dictionary, so the wire form stays
    ///     stable regardless of the JSON serializer's dictionary-key casing policy (which always camel-cases keys).
    ///     Argument names are the constants in <see cref="Vion.Contracts.Constants.RemoteAccessConstants.Arguments" />.
    /// </summary>
    public readonly record struct ServiceArgument(string Name, string Value);
}
