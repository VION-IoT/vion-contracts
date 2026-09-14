namespace Vion.Contracts.Hw.Modbus
{
    /// <summary>
    ///     Answers a <see cref="SetModbusPayload" />, published by the hardware-abstraction layer on the request's
    ///     response topic with content type <see cref="Vion.Contracts.Mqtt.MessageMimeTypes.Json" />.
    /// </summary>
    /// <param name="ResponseCode">The outcome of the write.</param>
    /// <param name="ErrorMessage">A human-readable diagnostic, or <c>null</c> when the write succeeded.</param>
    public record SetModbusResponsePayload(ModbusResponseCode ResponseCode, string? ErrorMessage);
}
