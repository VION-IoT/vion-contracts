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
    ///     For <see cref="AccessType.Publish" />, the filter is one exact topic under another gateway's installation
    ///     topic, without wildcards. What the provider publishes on that topic is forwarded to that gateway.
    /// </param>
    /// <param name="Access">What the service provider does on the topics <paramref name="TopicFilter" /> covers.</param>
    public readonly record struct TopicDeclaration(string TopicFilter, AccessType Access);

    public enum AccessType
    {
        Publish,
    }
}
