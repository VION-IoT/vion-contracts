using System.Text.Json.Serialization;

namespace Vion.Contracts.Hw.Modbus
{
    /// <summary>
    ///     Modbus function codes. The underlying values are the Modbus protocol wire values, but the JSON
    ///     representation is the member <b>name</b> — pinned by the <see cref="JsonStringEnumConverter" /> on the type
    ///     itself so the wire shape does not depend on the caller's <c>JsonSerializerOptions</c>.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ModbusFunctionCode : byte
    {
        /// <summary>No function code — the value an absent or unset field lands on.</summary>
        None = 0x00,

        /// <summary>FC01 — read coils.</summary>
        ReadCoils = 0x01,

        /// <summary>FC02 — read discrete inputs.</summary>
        ReadDiscreteInputs = 0x02,

        /// <summary>FC03 — read holding registers.</summary>
        ReadHoldingRegisters = 0x03,

        /// <summary>FC04 — read input registers.</summary>
        ReadInputRegisters = 0x04,

        /// <summary>FC05 — write single coil.</summary>
        WriteSingleCoil = 0x05,

        /// <summary>FC06 — write single register.</summary>
        WriteSingleRegister = 0x06,

        /// <summary>FC15 — write multiple coils.</summary>
        WriteMultipleCoils = 0x0F,

        /// <summary>FC16 — write multiple registers.</summary>
        WriteMultipleRegisters = 0x10,
    }
}
