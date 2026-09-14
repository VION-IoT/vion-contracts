namespace Vion.Contracts.Hw.Ao
{
    /// <summary>
    ///     Commands an analog output to a value, published by dale on
    ///     <see cref="Vion.Contracts.Mqtt.Topics.AoSet" /> with content type
    ///     <see cref="Vion.Contracts.Mqtt.MessageMimeTypes.Json" />. The target endpoint is identified by the topic.
    /// </summary>
    /// <param name="Value">
    ///     The value to drive, in the endpoint's engineering unit. May be non-finite, in which case it travels as a
    ///     quoted named literal (<c>"NaN"</c>, <c>"Infinity"</c>, <c>"-Infinity"</c>). See
    ///     <see cref="Vion.Contracts.Hw.HwJsonContext" />.
    /// </param>
    public record SetAoPayload(double Value);
}
