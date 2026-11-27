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
    ///     For <see cref="AccessType.Publish" />, the filter is one exact topic: another gateway's installation topic,
    ///     written exactly as that gateway's installation topic is, with its GUIDs in lower case with hyphens, followed by
    ///     the declaring service provider's identifier. It holds no wildcard, no empty level, no whitespace and no control
    ///     character. What the provider publishes on that topic is forwarded to that gateway.
    /// </param>
    /// <param name="Access">What the service provider does on the topics <paramref name="TopicFilter" /> covers.</param>
    public readonly record struct TopicDeclaration(string TopicFilter, AccessType Access);

    public enum AccessType
    {
        Publish,
    }
}
