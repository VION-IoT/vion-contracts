using System.Collections.Generic;

namespace Vion.Contracts.Events.ServiceProviderToMesh
{
    /// <summary>
    ///     The topics of other installations a service provider needs bridged. Each declaration replaces the previous
    ///     one whole.
    /// </summary>
    public readonly record struct BridgingDeclarationPayload(List<TopicDeclaration> Declarations);

    /// <summary>
    ///     One topic a service provider needs bridged.
    /// </summary>
    /// <param name="Topic">An MQTT topic filter beginning with another gateway's installation topic.</param>
    /// <param name="Access">How the service provider uses matching topics.</param>
    public readonly record struct TopicDeclaration(string Topic, AccessType Access);

    public enum AccessType
    {
        /// <summary>The service provider publishes on matching topics, and they are forwarded to that gateway.</summary>
        Publish,
    }
}
