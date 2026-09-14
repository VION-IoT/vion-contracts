using System.Text.Json.Serialization;

namespace Vion.Contracts.Hw.Modbus
{
    /// <summary>
    ///     Requests a Modbus write, published by dale on <see cref="Vion.Contracts.Mqtt.Topics.ModbusSet" /> with
    ///     content type <see cref="Vion.Contracts.Mqtt.MessageMimeTypes.Json" />. The hardware-abstraction layer
    ///     answers on the request's response topic with a <see cref="SetModbusResponsePayload" />.
    /// </summary>
    /// <param name="FunctionCode">The Modbus write function to execute.</param>
    /// <param name="UnitIdentifier">The Modbus unit (slave) address to address on the bus.</param>
    /// <param name="Address">
    ///     The target address: for a single coil / register the address itself, for the multiple variants the starting
    ///     address.
    /// </param>
    /// <param name="Data">
    ///     The raw bytes to write, big-endian as they go onto the bus. Encoded per <see cref="ModbusDataConverter" />.
    /// </param>
    public record SetModbusPayload(
        ModbusFunctionCode FunctionCode,
        byte UnitIdentifier,
        ushort Address,
        [property: JsonConverter(typeof(ModbusDataConverter))]
        byte[]? Data);
}
