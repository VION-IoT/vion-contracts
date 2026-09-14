using System;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        // WireOptions plus the one option the context adds. Comparing against this rather than only asserting the
        // literal strings is what shows the named-literal tokens are System.Text.Json's own spelling and not something
        // the context invented: a consumer that sets the same option on plain options gets the same bytes.
        private static readonly JsonSerializerOptions NonFiniteWireOptions = new()
                                                                             {
                                                                                 PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                                 DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                                                                                 NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
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
        public void CarryNonFiniteAnalogValuesAsTheNamedLiterals()
        {
            // A non-finite reading is ordinary in the field, not a bug: an unplugged or faulty sensor, or a
            // divide-by-zero in a scaling formula. dale's analog contract guarantees these pass through, and JSON has
            // no number token for them — the named literals, quoted, are the only way to carry one.
            AssertNamedLiteral(double.NaN, "NaN");
            AssertNamedLiteral(double.PositiveInfinity, "Infinity");
            AssertNamedLiteral(double.NegativeInfinity, "-Infinity");
        }

        [TestMethod]
        public void ShowWhatBreaksWithoutTheNamedLiteralOption()
        {
            // Guards the *reason* for NumberHandling on the context, not just its effect: WireOptions is the context's
            // options minus that one setting, so this is what the contracts shipped before the option was added.
            // Note the write failure is ArgumentException, not JsonException — a consumer that wraps its deserialise
            // in catch(JsonException) would not catch the publish side at all.
            Assert.Throws<ArgumentException>(() => JsonSerializer.Serialize(new AiStatePayload(double.NaN), WireOptions));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<AiStatePayload>("{\"value\":\"NaN\"}", WireOptions));
        }

        [TestMethod]
        public void KeepFiniteAnalogValuesAsBareNumbers()
        {
            // Allowing the named literals must not quote ordinary readings — that would be a silent wire break for
            // every finite value, which is all of them in normal operation.
            Assert.AreEqual("{\"value\":21.4}", JsonSerializer.Serialize(new AiStatePayload(21.4), HwJsonContext.Default.AiStatePayload));
            Assert.AreEqual("{\"value\":0}", JsonSerializer.Serialize(new AoStatePayload(0), HwJsonContext.Default.AoStatePayload));
            Assert.AreEqual("{\"value\":-1.25}", JsonSerializer.Serialize(new SetAoPayload(-1.25), HwJsonContext.Default.SetAoPayload));
        }

        [TestMethod]
        public void ExposeTheNamedLiteralOptionAsTheGeneratedNumberHandling()
        {
            Assert.AreEqual(JsonNumberHandling.AllowNamedFloatingPointLiterals, HwJsonContext.Default.Options.NumberHandling);
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

        // All three analog payloads must agree on the token: an input reading, an output reading, and a command.
        private static void AssertNamedLiteral(double value, string token)
        {
            var expectedJson = $"{{\"value\":\"{token}\"}}";

            AssertNonFiniteRoundTrip(new AiStatePayload(value), HwJsonContext.Default.AiStatePayload, expectedJson, payload => payload.Value);
            AssertNonFiniteRoundTrip(new AoStatePayload(value), HwJsonContext.Default.AoStatePayload, expectedJson, payload => payload.Value);
            AssertNonFiniteRoundTrip(new SetAoPayload(value), HwJsonContext.Default.SetAoPayload, expectedJson, payload => payload.Value);
        }

        private static void AssertNonFiniteRoundTrip<T>(T payload, JsonTypeInfo<T> typeInfo, string expectedJson, Func<T, double> valueOf)
        {
            var json = JsonSerializer.Serialize(payload, typeInfo);

            Assert.AreEqual(expectedJson, json, $"{typeof(T).Name} through HwJsonContext");

            // Not AssertMatchesReflection: WireOptions lacks NumberHandling and would throw. Comparing against plain
            // options that DO set it proves the token is System.Text.Json's own spelling, so a consumer on its own
            // options cannot silently differ from the context.
            Assert.AreEqual(expectedJson, JsonSerializer.Serialize(payload, NonFiniteWireOptions), $"{typeof(T).Name}: context and plain options disagree");

            var roundTripped = JsonSerializer.Deserialize(json, typeInfo)!;

            // double.Equals, not ==, because NaN != NaN but NaN.Equals(NaN) is true.
            Assert.IsTrue(valueOf(roundTripped).Equals(valueOf(payload)), $"{typeof(T).Name}: {valueOf(payload)} round-tripped to {valueOf(roundTripped)}");
            Assert.AreEqual(payload, roundTripped, $"{typeof(T).Name} record equality after round-trip");
            Assert.AreEqual(expectedJson, JsonSerializer.Serialize(roundTripped, typeInfo), $"{typeof(T).Name} re-serialise");
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
