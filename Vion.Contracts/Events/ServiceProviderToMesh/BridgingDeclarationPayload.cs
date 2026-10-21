using System.Collections.Generic;

namespace Vion.Contracts.Events.ServiceProviderToMesh
{
    /// <summary>
    ///     The topics of other installations a service provider needs bridged. Each declaration replaces the previous
    ///     one whole.
    /// </summary>
    public readonly record struct BridgingDeclarationPayload(List<TopicDeclaration> Declarations);

    /// <summary>
    ///     Topics of another installation a service provider needs bridged.
    /// </summary>
    /// <param name="TopicFilter">
    ///     An MQTT topic filter that starts with another installation's topic, written out literally;
    ///     wildcards are allowed only after it.
    /// </param>
    /// <param name="Access">What the service provider does on the topics <paramref name="TopicFilter" /> covers.</param>
    public readonly record struct TopicDeclaration(string TopicFilter, AccessType Access);

    public enum AccessType
    {
        /// <summary>The service provider publishes on them.</summary>
        Publish,
    }
}
