using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vion.Contracts.Hw.Modbus;

namespace Vion.Contracts.Test.Hw
{
    /// <summary>
    ///     Pins the wire shape of the hw/modbus payloads: the field sets, the base64 mapping of both byte vectors, and
    ///     the name-not-number representation of both enums. A silent flip in any of these is a wire break, and there
    ///     is no CI schema-diff check to catch it.
    /// </summary>
    [TestClass]
    public class ModbusPayloadsShould
    {
        private const string RegisterValue42Base64 = "ACo=";

        // Deliberately WITHOUT JsonStringEnumConverter: the enums pin their own representation via an attribute on
        // the type, so the wire shape must not depend on what the caller configures.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        // 0x00 0x2A — big-endian for register value 42, which is "ACo=" in base64.
        private static readonly byte[] RegisterValue42 = [0x00, 0x2A];

        [TestMethod]
        public void SerialiseAReadRequestWithItsFourRequestParameters()
        {
            var payload = new GetModbusPayload(ModbusFunctionCode.ReadHoldingRegisters, 1, 100, 2);

            Assert.AreEqual("{\"functionCode\":\"ReadHoldingRegisters\",\"unitIdentifier\":1,\"startingAddress\":100,\"quantity\":2}",
                            JsonSerializer.Serialize(payload, WireOptions));
        }

        [TestMethod]
        public void SerialiseAWriteRequestWithItsDataAsBase64()
        {
            var payload = new SetModbusPayload(ModbusFunctionCode.WriteSingleRegister, 1, 100, RegisterValue42);

            Assert.AreEqual($"{{\"functionCode\":\"WriteSingleRegister\",\"unitIdentifier\":1,\"address\":100,\"data\":\"{RegisterValue42Base64}\"}}",
                            JsonSerializer.Serialize(payload, WireOptions));
        }

        [TestMethod]
        public void SerialiseAReadResponseWithItsDataAsBase64()
        {
            var payload = new GetModbusResponsePayload(ModbusResponseCode.Ok, null, RegisterValue42);

            Assert.AreEqual($"{{\"responseCode\":\"Ok\",\"errorMessage\":null,\"data\":\"{RegisterValue42Base64}\"}}", JsonSerializer.Serialize(payload, WireOptions));
        }

        [TestMethod]
        public void SerialiseAWriteResponseWithItsTwoFields()
        {
            var payload = new SetModbusResponsePayload(ModbusResponseCode.IllegalDataAddress, "address 100 is out of range");

            Assert.AreEqual("{\"responseCode\":\"IllegalDataAddress\",\"errorMessage\":\"address 100 is out of range\"}", JsonSerializer.Serialize(payload, WireOptions));
        }

        [TestMethod]
        public void EncodeBothByteVectorsIdentically()
        {
            // ModbusDataConverter is the single switch point for this encoding (spec item S-6 may swap base64 for an
            // integer array); this asserts the two vectors cannot drift apart.
            var request = JsonNode.Parse(JsonSerializer.Serialize(new SetModbusPayload(ModbusFunctionCode.WriteSingleRegister, 1, 100, RegisterValue42), WireOptions))!;
            var response = JsonNode.Parse(JsonSerializer.Serialize(new GetModbusResponsePayload(ModbusResponseCode.Ok, null, RegisterValue42), WireOptions))!;

            Assert.AreEqual(request["data"]!.ToJsonString(), response["data"]!.ToJsonString());
        }

        [TestMethod]
        public void CarryAbsentDataAsNullNotAsAnEmptyVector()
        {
            var failed = new GetModbusResponsePayload(ModbusResponseCode.ServerDeviceFailure, "bus timeout", null);

            Assert.AreEqual("{\"responseCode\":\"ServerDeviceFailure\",\"errorMessage\":\"bus timeout\",\"data\":null}", JsonSerializer.Serialize(failed, WireOptions));
            Assert.IsNull(JsonSerializer.Deserialize<GetModbusResponsePayload>(JsonSerializer.Serialize(failed, WireOptions), WireOptions)!.Data);
        }

        [TestMethod]
        public void RoundtripBothByteVectors()
        {
            var request = new SetModbusPayload(ModbusFunctionCode.WriteMultipleRegisters, 7, 4096, [0x01, 0x02, 0x03, 0x04]);
            var response = new GetModbusResponsePayload(ModbusResponseCode.Ok, null, [0xFF, 0x00]);

            CollectionAssert.AreEqual(request.Data, Roundtrip(request).Data);
            CollectionAssert.AreEqual(response.Data, Roundtrip(response).Data);
        }

        [TestMethod]
        public void SerialiseEveryFunctionCodeAsItsMemberName()
        {
            foreach (ModbusFunctionCode functionCode in Enum.GetValues(typeof(ModbusFunctionCode)))
            {
                Assert.AreEqual($"\"{functionCode}\"", JsonSerializer.Serialize(functionCode, WireOptions));
                Assert.AreEqual(functionCode, JsonSerializer.Deserialize<ModbusFunctionCode>($"\"{functionCode}\"", WireOptions));
            }
        }

        [TestMethod]
        public void SerialiseEveryResponseCodeAsItsMemberName()
        {
            foreach (ModbusResponseCode responseCode in Enum.GetValues(typeof(ModbusResponseCode)))
            {
                Assert.AreEqual($"\"{responseCode}\"", JsonSerializer.Serialize(responseCode, WireOptions));
                Assert.AreEqual(responseCode, JsonSerializer.Deserialize<ModbusResponseCode>($"\"{responseCode}\"", WireOptions));
            }
        }

        [TestMethod]
        public void KeepTheModbusProtocolValuesAsTheUnderlyingFunctionCodeValues()
        {
            // The JSON carries names, but the numbers stay the Modbus wire values so a service provider can cast
            // straight onto the bus. Every member is pinned, and the count guards against an unpinned addition.
            var wireValues = new Dictionary<ModbusFunctionCode, byte>
                             {
                                 [ModbusFunctionCode.None] = 0x00,
                                 [ModbusFunctionCode.ReadCoils] = 0x01,
                                 [ModbusFunctionCode.ReadDiscreteInputs] = 0x02,
                                 [ModbusFunctionCode.ReadHoldingRegisters] = 0x03,
                                 [ModbusFunctionCode.ReadInputRegisters] = 0x04,
                                 [ModbusFunctionCode.WriteSingleCoil] = 0x05,
                                 [ModbusFunctionCode.WriteSingleRegister] = 0x06,
                                 [ModbusFunctionCode.WriteMultipleCoils] = 0x0F,
                                 [ModbusFunctionCode.WriteMultipleRegisters] = 0x10,
                             };

            AssertUnderlyingValues(wireValues);
        }

        [TestMethod]
        public void KeepTheModbusExceptionCodesAsTheUnderlyingResponseCodeValues()
        {
            var wireValues = new Dictionary<ModbusResponseCode, byte>
                             {
                                 [ModbusResponseCode.Ok] = 0x00,
                                 [ModbusResponseCode.IllegalFunction] = 0x01,
                                 [ModbusResponseCode.IllegalDataAddress] = 0x02,
                                 [ModbusResponseCode.IllegalDataValue] = 0x03,
                                 [ModbusResponseCode.ServerDeviceFailure] = 0x04,
                                 [ModbusResponseCode.Acknowledge] = 0x05,
                                 [ModbusResponseCode.ServerDeviceBusy] = 0x06,
                                 [ModbusResponseCode.MemoryParityError] = 0x08,
                                 [ModbusResponseCode.GatewayPathUnavailable] = 0x0A,
                                 [ModbusResponseCode.GatewayTargetDeviceFailedToRespond] = 0x0B,
                                 [ModbusResponseCode.NonProtocolError] = 0xFF,
                             };

            AssertUnderlyingValues(wireValues);
        }

        [TestMethod]
        public void ReadTheHandWrittenPayloadAStructuredTextProducerWouldEmit()
        {
            const string handWritten = "{\"responseCode\":\"Ok\",\"errorMessage\":null,\"data\":\"ACo=\"}";

            var payload = JsonSerializer.Deserialize<GetModbusResponsePayload>(handWritten, WireOptions)!;

            Assert.AreEqual(ModbusResponseCode.Ok, payload.ResponseCode);
            Assert.IsNull(payload.ErrorMessage);
            CollectionAssert.AreEqual(RegisterValue42, payload.Data);
        }

        private static T Roundtrip<T>(T payload)
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(payload, WireOptions), WireOptions)!;
        }

        private static void AssertUnderlyingValues<TEnum>(Dictionary<TEnum, byte> wireValues)
            where TEnum : struct, Enum
        {
            Assert.HasCount(Enum.GetValues<TEnum>().Length, wireValues, $"every {typeof(TEnum).Name} member must pin its Modbus wire value");
            foreach (var (member, wireValue) in wireValues)
            {
                Assert.AreEqual(wireValue, Convert.ToByte(member), $"{typeof(TEnum).Name}.{member}");
            }
        }
    }
}
