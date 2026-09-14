using System.Text.Json.Serialization;

namespace Vion.Contracts.Hw.Modbus
{
    /// <summary>
    ///     Modbus function codes. The underlying values are the Modbus protocol wire values, but the JSON
    ///     representation is the member <b>name</b> — pinned by the <see cref="JsonStringEnumConverter{TEnum}" /> on
    ///     the type itself so the wire shape does not depend on the caller's <c>JsonSerializerOptions</c>.
    ///     <para>
    ///         The converter is the <b>generic</b> one on purpose: the non-generic <c>JsonStringEnumConverter</c> is
    ///         <c>[RequiresDynamicCode]</c>, so it cannot be used from a source-generated context on a NativeAOT
    ///         consumer — the generator reports <c>SYSLIB1034</c> and the converter fails at run time once trimmed.
    ///     </para>
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ModbusFunctionCode>))]
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
