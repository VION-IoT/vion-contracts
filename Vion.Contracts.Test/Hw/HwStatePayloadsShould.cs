using System.Text.Json;
using Vion.Contracts.Hw.Ai;
using Vion.Contracts.Hw.Ao;
using Vion.Contracts.Hw.Di;
using Vion.Contracts.Hw.Do;

namespace Vion.Contracts.Test.Hw
{
    /// <summary>
    ///     Pins the wire shape of the hw/* state and set payloads. Identity (hardware block instance, endpoint) lives
    ///     in the MQTT topic and must NOT reappear in the payload — these assertions are exact-string, so a field
    ///     creeping back in fails here rather than silently on the wire. There is no CI schema-diff check.
    /// </summary>
    [TestClass]
    public class HwStatePayloadsShould
    {
        // Mirrors the platform wire convention: camelCase property names + dictionary keys.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void SerialiseADigitalInputStateAsValueAndNothingElse()
        {
            Assert.AreEqual("{\"value\":true}", JsonSerializer.Serialize(new DiStatePayload(true), WireOptions));
            Assert.AreEqual("{\"value\":false}", JsonSerializer.Serialize(new DiStatePayload(false), WireOptions));
        }

        [TestMethod]
        public void SerialiseADigitalOutputStateAsValueAndNothingElse()
        {
            Assert.AreEqual("{\"value\":true}", JsonSerializer.Serialize(new DoStatePayload(true), WireOptions));
        }

        [TestMethod]
        public void SerialiseAnAnalogInputStateAsValueAndNothingElse()
        {
            Assert.AreEqual("{\"value\":21.4}", JsonSerializer.Serialize(new AiStatePayload(21.4), WireOptions));
        }

        [TestMethod]
        public void SerialiseAnAnalogOutputStateAsValueAndNothingElse()
        {
            Assert.AreEqual("{\"value\":42.5}", JsonSerializer.Serialize(new AoStatePayload(42.5), WireOptions));
        }

        [TestMethod]
        public void GiveSetPayloadsTheSameShapeAsTheirStateCounterparts()
        {
            // The symmetry is the point of dropping the identity fields: a set and a state on the same endpoint
            // differ only by topic.
            Assert.AreEqual(JsonSerializer.Serialize(new DoStatePayload(true), WireOptions), JsonSerializer.Serialize(new SetDoPayload(true), WireOptions));
            Assert.AreEqual(JsonSerializer.Serialize(new AoStatePayload(42.5), WireOptions), JsonSerializer.Serialize(new SetAoPayload(42.5), WireOptions));
        }

        [TestMethod]
        public void RoundtripEveryStateAndSetPayload()
        {
            Assert.AreEqual(new DiStatePayload(true), Roundtrip(new DiStatePayload(true)));
            Assert.AreEqual(new DoStatePayload(true), Roundtrip(new DoStatePayload(true)));
            Assert.AreEqual(new SetDoPayload(false), Roundtrip(new SetDoPayload(false)));
            Assert.AreEqual(new AiStatePayload(21.4), Roundtrip(new AiStatePayload(21.4)));
            Assert.AreEqual(new AoStatePayload(42.5), Roundtrip(new AoStatePayload(42.5)));
            Assert.AreEqual(new SetAoPayload(-1.25), Roundtrip(new SetAoPayload(-1.25)));
        }

        [TestMethod]
        public void ReadTheHandWrittenPayloadAStructuredTextProducerWouldEmit()
        {
            // The TwinCAT service provider composes this JSON by hand; deserialisation must not depend on any
            // field beyond `value`.
            Assert.IsTrue(JsonSerializer.Deserialize<DiStatePayload>("{\"value\":true}", WireOptions)!.Value);
            Assert.AreEqual(21.4, JsonSerializer.Deserialize<AiStatePayload>("{\"value\":21.4}", WireOptions)!.Value);
        }

        private static T Roundtrip<T>(T payload)
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(payload, WireOptions), WireOptions)!;
        }
    }
}
