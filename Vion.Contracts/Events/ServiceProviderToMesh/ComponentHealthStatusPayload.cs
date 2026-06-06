using Vion.Contracts.Events.MeshToCloud;

namespace Vion.Contracts.Events.ServiceProviderToMesh
{
    /// <summary>
    ///     Represents the health status of a single component as reported by a service provider to Mesh.
    ///     This is the single-component variant of <see cref="SystemHealthStatusPayload" /> (which carries a
    ///     <see cref="System.Collections.Generic.List{T}" /> of components) and the JSON counterpart of the FlatBuffers
    ///     <c>ComponentHealthStatusPayload</c>, which carries the same single component over the binary wire.
    /// </summary>
    /// <param name="Component">
    ///     The component being reported, including its connection/health status, the timestamp it entered that state,
    ///     an optional reason, and any subcomponents. See <see cref="MeshToCloud.Component" /> for field semantics.
    /// </param>
    [Schema("ComponentHealthStatusPayload")]
    public record ComponentHealthStatusPayload(Component Component) : IMessage;
}
