namespace Vion.Contracts.Hw.Modbus
{
    /// <summary>
    ///     Requests a Modbus read, published by dale on <see cref="Vion.Contracts.Mqtt.Topics.ModbusGet" /> with
    ///     content type <see cref="Vion.Contracts.Mqtt.MessageMimeTypes.Json" />. The hardware-abstraction layer
    ///     answers on the request's response topic with a <see cref="GetModbusResponsePayload" />.
    /// </summary>
    /// <param name="FunctionCode">The Modbus read function to execute.</param>
    /// <param name="UnitIdentifier">The Modbus unit (slave) address to address on the bus.</param>
    /// <param name="StartingAddress">The address of the first coil / register to read.</param>
    /// <param name="Quantity">The number of coils / registers to read.</param>
    public record GetModbusPayload(ModbusFunctionCode FunctionCode, byte UnitIdentifier, ushort StartingAddress, ushort Quantity);
}
