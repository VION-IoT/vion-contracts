using System.Text.Json.Serialization;
using Vion.Contracts.Hw.Ai;
using Vion.Contracts.Hw.Ao;
using Vion.Contracts.Hw.Di;
using Vion.Contracts.Hw.Do;
using Vion.Contracts.Hw.Modbus;

// ReSharper disable PartialTypeWithSinglePart this must be a partial class because it is generated

namespace Vion.Contracts.Hw
{
    /// <summary>
    ///     Source-generation context for every <c>hw/*</c> payload, shipped here so that the hardware-abstraction
    ///     layers and dale share one AOT-safe entry point instead of each declaring their own — a second context would
    ///     be a second place for the wire options to drift.
    ///     <para>
    ///         Both NativeAOT hardware-abstraction layers publish with reflection trimmed away, so the reflection-based
    ///         <c>JsonSerializer.Serialize(object, Type)</c> overloads are not available to them. Going through
    ///         <see cref="Default" /> — <c>JsonSerializer.Serialize(payload, HwJsonContext.Default.DiStatePayload)</c> —
    ///         keeps them free of <c>IL2026</c> / <c>IL3050</c>.
    ///     </para>
    ///     <para>
    ///         The options here mirror the platform wire convention (camelCase property names and dictionary keys), the
    ///         same pair dale configures in <c>Vion.Dale.Sdk.Mqtt.JsonSerialization.DefaultOptions</c>. Enums are
    ///         deliberately <b>not</b> listed under <c>Converters</c>: <see cref="ModbusFunctionCode" /> and
    ///         <see cref="ModbusResponseCode" /> pin their name-not-number representation with a
    ///         <see cref="JsonConverterAttribute" /> on the type itself, which this context honours. Repeating them
    ///         here would be a second source of truth for the same decision, and only one of the two would be read.
    ///     </para>
    ///     <para>
    ///         <c>NumberHandling</c> allows the <b>named floating-point literals</b>, so a non-finite analog value
    ///         travels as <c>"NaN"</c>, <c>"Infinity"</c> or <c>"-Infinity"</c> — a quoted string, which is the only
    ///         thing JSON can carry for them. Without it an unplugged sensor or a divide-by-zero in a scaling formula
    ///         breaks the publish instead of the reading: <c>double.NaN</c> throws <c>ArgumentException</c> on write
    ///         (not even <c>JsonException</c>, so a consumer's <c>catch</c> misses it) and the quoted form cannot be
    ///         read back. dale's analog contract guarantees these values pass through, so the guarantee is kept here
    ///         rather than in each consumer's own options. Symmetric: the option governs both read and write.
    ///     </para>
    /// </summary>
    [JsonSerializable(typeof(DiStatePayload))]
    [JsonSerializable(typeof(DoStatePayload))]
    [JsonSerializable(typeof(SetDoPayload))]
    [JsonSerializable(typeof(AiStatePayload))]
    [JsonSerializable(typeof(AoStatePayload))]
    [JsonSerializable(typeof(SetAoPayload))]
    [JsonSerializable(typeof(GetModbusPayload))]
    [JsonSerializable(typeof(GetModbusResponsePayload))]
    [JsonSerializable(typeof(SetModbusPayload))]
    [JsonSerializable(typeof(SetModbusResponsePayload))]

    // The two enums are serializable in their own right so a caller can round-trip a bare function or response code
    // through the context, not only as a field of a payload.
    [JsonSerializable(typeof(ModbusFunctionCode))]
    [JsonSerializable(typeof(ModbusResponseCode))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                                 DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
                                 NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals)]
    public partial class HwJsonContext : JsonSerializerContext;
}
