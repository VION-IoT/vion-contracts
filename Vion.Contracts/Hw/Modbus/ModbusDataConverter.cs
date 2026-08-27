using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vion.Contracts.Hw.Modbus
{
    /// <summary>
    ///     The single authority for how Modbus register/coil payload bytes are represented in JSON: a base64 string.
    ///     <para>
    ///         Both byte vectors on the Modbus contract — <see cref="SetModbusPayload.Data" /> and
    ///         <see cref="GetModbusResponsePayload.Data" /> — are annotated with this converter, so the encoding is
    ///         stated explicitly on the type rather than inherited from a <c>System.Text.Json</c> default, and it does
    ///         not depend on the caller's <c>JsonSerializerOptions</c>.
    ///     </para>
    ///     <para>
    ///         <b>This class is the switch point.</b> The TwinCAT spike (spec item S-6) may find base64 awkward to
    ///         produce in Structured Text and ask for an array of integers instead; that is a change to
    ///         <see cref="Read" /> and <see cref="Write" /> here, and both vectors follow — there is nowhere else to
    ///         edit.
    ///     </para>
    /// </summary>
    public class ModbusDataConverter : JsonConverter<byte[]>
    {
        /// <inheritdoc />
        public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetBytesFromBase64();
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            writer.WriteBase64StringValue(value);
        }
    }
}
