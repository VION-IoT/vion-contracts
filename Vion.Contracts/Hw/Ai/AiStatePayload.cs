namespace Vion.Contracts.Hw.Ai
{
    /// <summary>
    ///     Reports the current value of an analog input, published by the hardware-abstraction layer on
    ///     <see cref="Vion.Contracts.Mqtt.Topics.AiState" /> with content type
    ///     <see cref="Vion.Contracts.Mqtt.MessageMimeTypes.Json" />.
    ///     <para>
    ///         The endpoint identity is carried by the topic
    ///         (<c>{installationTopic}/{spId}/{service}/{contract}/hw/ai/state</c>) and is deliberately absent from the
    ///         payload, so the topic is the single authority for it and the two can never disagree. A captured message
    ///         is therefore only interpretable together with its topic.
    ///     </para>
    /// </summary>
    /// <param name="Value">
    ///     The input value in the endpoint's engineering unit. May be non-finite — an unplugged or faulty sensor, or a
    ///     divide-by-zero in a scaling formula, is ordinary in the field — in which case it travels as a quoted named
    ///     literal (<c>"NaN"</c>, <c>"Infinity"</c>, <c>"-Infinity"</c>). See <see cref="Vion.Contracts.Hw.HwJsonContext" />.
    /// </param>
    public record AiStatePayload(double Value);
}
