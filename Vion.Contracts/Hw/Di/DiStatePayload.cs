namespace Vion.Contracts.Hw.Di
{
    /// <summary>
    ///     Reports the current level of a digital input, published by the hardware-abstraction layer on
    ///     <see cref="Vion.Contracts.Mqtt.Topics.DiState" /> with content type
    ///     <see cref="Vion.Contracts.Mqtt.MessageMimeTypes.Json" />.
    ///     <para>
    ///         The endpoint identity is carried by the topic
    ///         (<c>{installationTopic}/{spId}/{service}/{contract}/hw/di/state</c>) and is deliberately absent from the
    ///         payload, so the topic is the single authority for it and the two can never disagree. A captured message
    ///         is therefore only interpretable together with its topic.
    ///     </para>
    /// </summary>
    /// <param name="Value">The input level: <c>true</c> = high, <c>false</c> = low.</param>
    public record DiStatePayload(bool Value);
}
