namespace Vion.Contracts.Hw.Do
{
    /// <summary>
    ///     Reports the current level of a digital output, published by the hardware-abstraction layer on
    ///     <see cref="Vion.Contracts.Mqtt.Topics.DoState" /> with content type
    ///     <see cref="Vion.Contracts.Mqtt.MessageMimeTypes.Json" />. Symmetric with <see cref="SetDoPayload" />.
    ///     <para>
    ///         The endpoint identity is carried by the topic
    ///         (<c>{installationTopic}/{spId}/{service}/{contract}/hw/do/state</c>) and is deliberately absent from the
    ///         payload, so the topic is the single authority for it and the two can never disagree. A captured message
    ///         is therefore only interpretable together with its topic.
    ///     </para>
    /// </summary>
    /// <param name="Value">The output level: <c>true</c> = high, <c>false</c> = low.</param>
    public record DoStatePayload(bool Value);
}
