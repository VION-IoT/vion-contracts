using System.Text.Json.Serialization;

namespace Vion.Contracts.Hw.Modbus
{
    /// <summary>
    ///     Answers a <see cref="GetModbusPayload" />, published by the hardware-abstraction layer on the request's
    ///     response topic with content type <see cref="Vion.Contracts.Mqtt.MessageMimeTypes.Json" />.
    /// </summary>
    /// <param name="ResponseCode">The outcome of the read. Anything but <see cref="ModbusResponseCode.Ok" /> means no data.</param>
    /// <param name="ErrorMessage">A human-readable diagnostic, or <c>null</c> when the read succeeded.</param>
    /// <param name="Data">
    ///     The raw response bytes, big-endian as they came off the bus, or <c>null</c> when the read failed.
    ///     Encoded per <see cref="ModbusDataConverter" />.
    /// </param>
    public record GetModbusResponsePayload(
        ModbusResponseCode ResponseCode,
        string? ErrorMessage,
        [property: JsonConverter(typeof(ModbusDataConverter))]
        byte[]? Data);
}
