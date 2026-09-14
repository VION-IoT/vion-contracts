using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Vion.Contracts.Hw;
using Vion.Contracts.Hw.Ai;
using Vion.Contracts.Hw.Ao;
using Vion.Contracts.Hw.Di;
using Vion.Contracts.Hw.Do;
using Vion.Contracts.Hw.Modbus;

namespace Vion.Contracts.Test.Hw
{
    /// <summary>
    ///     Pins that the source-generated <see cref="HwJsonContext" /> puts the same bytes on the wire as the
    ///     reflection-based path the other two <c>Hw</c> test classes exercise. Two serialisers for one contract is two
    ///     chances to drift, and the NativeAOT hardware-abstraction layers can only use this one — so a divergence
    ///     would show up in the field, on the platform nobody can debug with a JIT test.
    ///     <para>
    ///         Whether the context is AOT-<i>safe</i> is not provable here: a reflection-based converter works
    ///         perfectly under JIT and only fails once trimmed. That proof is <c>Vion.Contracts.AotProof</c>, published
    ///         with <c>PublishAot</c> and run — see <c>.github/workflows/aot-proof.yml</c>. These tests cover the other
    ///         question, which is whether the wire shape is identical.
    ///     </para>
    /// </summary>
    [TestClass]
    public class HwJsonContextShould
    {
        // The reflection-based options the rest of the Hw tests use, and the ones dale configures in
        // Vion.Dale.Sdk.Mqtt.JsonSerialization.DefaultOptions. The context must agree with these.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void SerialiseEveryStateAndSetPayloadExactlyAsTheReflectionPathDoes()
        {
            AssertMatchesReflection(new DiStatePayload(true), HwJsonContext.Default.DiStatePayload, "{\"value\":true}");
            AssertMatchesReflection(new DoStatePayload(true), HwJsonContext.Default.DoStatePayload, "{\"value\":true}");
            AssertMatchesReflection(new SetDoPayload(false), HwJsonContext.Default.SetDoPayload, "{\"value\":false}");
            AssertMatchesReflection(new AiStatePayload(21.4), HwJsonContext.Default.AiStatePayload, "{\"value\":21.4}");
            AssertMatchesReflection(new AoStatePayload(42.5), HwJsonContext.Default.AoStatePayload, "{\"value\":42.5}");
            AssertMatchesReflection(new SetAoPayload(42.5), HwJsonContext.Default.SetAoPayload, "{\"value\":42.5}");
        }

        [TestMethod]
        public void SerialiseEveryModbusPayloadExactlyAsTheReflectionPathDoes()
        {
            AssertMatchesReflection(new GetModbusPayload(ModbusFunctionCode.ReadHoldingRegisters, 3, 40001, 2),
                                    HwJsonContext.Default.GetModbusPayload,
                                    "{\"functionCode\":\"ReadHoldingRegisters\",\"unitIdentifier\":3,\"startingAddress\":40001,\"quantity\":2}");

            AssertMatchesReflection(new GetModbusResponsePayload(ModbusResponseCode.Ok, null, [0x00, 0x01, 0x00, 0x02]),
                                    HwJsonContext.Default.GetModbusResponsePayload,
                                    "{\"responseCode\":\"Ok\",\"errorMessage\":null,\"data\":\"AAEAAg==\"}");

            AssertMatchesReflection(new SetModbusPayload(ModbusFunctionCode.WriteMultipleRegisters, 3, 40001, [0x00, 0x01, 0x00, 0x02]),
                                    HwJsonContext.Default.SetModbusPayload,
                                    "{\"functionCode\":\"WriteMultipleRegisters\",\"unitIdentifier\":3,\"address\":40001,\"data\":\"AAEAAg==\"}");

            AssertMatchesReflection(new SetModbusResponsePayload(ModbusResponseCode.Ok, null),
                                    HwJsonContext.Default.SetModbusResponsePayload,
                                    "{\"responseCode\":\"Ok\",\"errorMessage\":null}");
        }

        [TestMethod]
        public void CarryTheCustomDataConverterThroughTheGeneratedMetadata()
        {
            // ModbusDataConverter is attached with a property-level [JsonConverter]; a source generator that missed it
            // would silently fall back to System.Text.Json's own byte[] handling. Round-tripping through the context
            // and comparing the bytes is what catches that.
            var response = new GetModbusResponsePayload(ModbusResponseCode.Ok, null, [0xFF, 0x00, 0x2A]);

            var json = JsonSerializer.Serialize(response, HwJsonContext.Default.GetModbusResponsePayload);
            var roundTripped = JsonSerializer.Deserialize(json, HwJsonContext.Default.GetModbusResponsePayload)!;

            CollectionAssert.AreEqual(response.Data, roundTripped.Data);
        }

        [TestMethod]
        public void KeepAbsentDataNullThroughTheContext()
        {
            var failed = new SetModbusPayload(ModbusFunctionCode.WriteSingleCoil, 1, 100, null);

            var json = JsonSerializer.Serialize(failed, HwJsonContext.Default.SetModbusPayload);

            Assert.AreEqual("{\"functionCode\":\"WriteSingleCoil\",\"unitIdentifier\":1,\"address\":100,\"data\":null}", json);
            Assert.IsNull(JsonSerializer.Deserialize(json, HwJsonContext.Default.SetModbusPayload)!.Data);
        }

        [TestMethod]
        public void RejectMalformedDataAsAJsonExceptionThroughTheContextToo()
        {
            // Same contract as the reflection path (see ModbusPayloadsShould): a hand-written Structured Text producer
            // is a live source of malformed base64, and a consumer only wraps JsonException.
            const string notBase64 = "{\"responseCode\":\"Ok\",\"errorMessage\":null,\"data\":\"not base64!\"}";
            const string notAString = "{\"responseCode\":\"Ok\",\"errorMessage\":null,\"data\":[0,42]}";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(notBase64, HwJsonContext.Default.GetModbusResponsePayload));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(notAString, HwJsonContext.Default.GetModbusResponsePayload));
        }

        [TestMethod]
        public void SerialiseEveryFunctionCodeAsItsMemberNameThroughTheContext()
        {
            // The generic JsonStringEnumConverter<TEnum> sits on the enum type, so the context inherits it without
            // listing it under JsonSourceGenerationOptions.Converters — this asserts that inheritance actually happens.
            foreach (var functionCode in Enum.GetValues<ModbusFunctionCode>())
            {
                AssertMatchesReflection(functionCode, HwJsonContext.Default.ModbusFunctionCode, $"\"{functionCode}\"");
            }
        }

        [TestMethod]
        public void SerialiseEveryResponseCodeAsItsMemberNameThroughTheContext()
        {
            foreach (var responseCode in Enum.GetValues<ModbusResponseCode>())
            {
                AssertMatchesReflection(responseCode, HwJsonContext.Default.ModbusResponseCode, $"\"{responseCode}\"");
            }
        }

        [TestMethod]
        public void ExposeCamelCaseAsTheGeneratedNamingPolicy()
        {
            // The options are baked into the generated context at compile time; a caller cannot pass them in. Pinning
            // them here means a change to the attribute fails a test rather than only showing up on the wire.
            Assert.AreEqual(JsonNamingPolicy.CamelCase, HwJsonContext.Default.Options.PropertyNamingPolicy);
            Assert.AreEqual(JsonNamingPolicy.CamelCase, HwJsonContext.Default.Options.DictionaryKeyPolicy);
        }

        private static void AssertMatchesReflection<T>(T value, JsonTypeInfo<T> typeInfo, string expectedJson)
        {
            var throughContext = JsonSerializer.Serialize(value, typeInfo);

            Assert.AreEqual(expectedJson, throughContext, $"{typeof(T).Name} through HwJsonContext");
            Assert.AreEqual(JsonSerializer.Serialize(value, WireOptions), throughContext, $"{typeof(T).Name}: context and reflection disagree");

            var roundTripped = JsonSerializer.Deserialize(expectedJson, typeInfo)!;

            Assert.AreEqual(expectedJson, JsonSerializer.Serialize(roundTripped, typeInfo), $"{typeof(T).Name} round-trip");
        }
    }
}
