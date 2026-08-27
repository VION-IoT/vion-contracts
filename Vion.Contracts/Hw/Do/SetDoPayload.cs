namespace Vion.Contracts.Hw.Do
{
    /// <summary>
    ///     Commands a digital output to a level, published by dale on
    ///     <see cref="Vion.Contracts.Mqtt.Topics.DoSet" /> with content type
    ///     <see cref="Vion.Contracts.Mqtt.MessageMimeTypes.Json" />. The target endpoint is identified by the topic.
    /// </summary>
    /// <param name="Value">The level to drive: <c>true</c> = high, <c>false</c> = low.</param>
    public record SetDoPayload(bool Value);
}
