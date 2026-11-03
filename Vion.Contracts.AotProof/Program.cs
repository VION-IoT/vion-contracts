using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Vion.Contracts.Events;
using Vion.Contracts.Events.MeshToCloud;
using Vion.Contracts.Events.ServiceProviderToMesh;
using Vion.Contracts.Hw;
using Vion.Contracts.Hw.Ai;
using Vion.Contracts.Hw.Ao;
using Vion.Contracts.Hw.Di;
using Vion.Contracts.Hw.Do;
using Vion.Contracts.Hw.Modbus;

namespace Vion.Contracts.AotProof
{
    /// <summary>
    ///     Drives every <c>hw/*</c> payload and both Modbus enums through <see cref="HwJsonContext" />, and a set of
    ///     <c>Events/*</c> payloads through <see cref="EventsJsonContext" />, and asserts the result, so that publishing
    ///     this project with <c>PublishAot=true</c> and running the binary is a proof that both contexts are usable from a
    ///     NativeAOT consumer — the hardware-abstraction layers for the first, mesh for the second.
    ///     <para>
    ///         Every call goes through a <see cref="JsonTypeInfo{T}" /> off <see cref="HwJsonContext.Default" /> — the
    ///         overloads that take only <c>JsonSerializerOptions</c> are the reflection-based ones and are exactly what
    ///         a NativeAOT consumer cannot use. Each payload is checked three ways: the JSON matches the expected wire
    ///         string, the value survives a round-trip, and re-serialising the round-tripped value reproduces the same
    ///         JSON.
    ///     </para>
    /// </summary>
    public static class Program
    {
        private static readonly List<string> Failures = [];

        /// <summary>
        ///     Runs every check and returns 0 when all of them pass, 1 otherwise — a failed assertion must fail the
        ///     process, not just print.
        /// </summary>
        public static int Main()
        {
            CheckHwStatePayloads();
            CheckModbusPayloads();
            CheckEnums();
            CheckEventsPayloads();

            if (Failures.Count > 0)
            {
                Console.Error.WriteLine($"AOT proof FAILED — {Failures.Count} assertion(s):");
                foreach (var failure in Failures)
                {
                    Console.Error.WriteLine($"  {failure}");
                }

                return 1;
            }

            Console.WriteLine("AOT proof passed: 10 records + 2 enums round-tripped through HwJsonContext, 5 payloads through EventsJsonContext.");

            return 0;
        }

        private static void CheckHwStatePayloads()
        {
            Check(new DiStatePayload(true), HwJsonContext.Default.DiStatePayload, "{\"value\":true}");
            Check(new DiStatePayload(false), HwJsonContext.Default.DiStatePayload, "{\"value\":false}");
            Check(new DoStatePayload(true), HwJsonContext.Default.DoStatePayload, "{\"value\":true}");
            Check(new SetDoPayload(false), HwJsonContext.Default.SetDoPayload, "{\"value\":false}");
            Check(new AiStatePayload(21.4), HwJsonContext.Default.AiStatePayload, "{\"value\":21.4}");
            Check(new AoStatePayload(42.5), HwJsonContext.Default.AoStatePayload, "{\"value\":42.5}");
            Check(new SetAoPayload(-1.25), HwJsonContext.Default.SetAoPayload, "{\"value\":-1.25}");
        }

        private static void CheckModbusPayloads()
        {
            Check(new GetModbusPayload(ModbusFunctionCode.ReadHoldingRegisters, 3, 40001, 2),
                  HwJsonContext.Default.GetModbusPayload,
                  "{\"functionCode\":\"ReadHoldingRegisters\",\"unitIdentifier\":3,\"startingAddress\":40001,\"quantity\":2}");

            // The two byte vectors are the reason the context matters most: ModbusDataConverter is a custom
            // JsonConverter<byte[]>, and a source-generated context has to pick it up off the property attribute.
            Check(new GetModbusResponsePayload(ModbusResponseCode.Ok, null, [0x00, 0x01, 0x00, 0x02]),
                  HwJsonContext.Default.GetModbusResponsePayload,
                  "{\"responseCode\":\"Ok\",\"errorMessage\":null,\"data\":\"AAEAAg==\"}",
                  SameGetResponse);

            Check(new GetModbusResponsePayload(ModbusResponseCode.ServerDeviceFailure, "bus timeout", null),
                  HwJsonContext.Default.GetModbusResponsePayload,
                  "{\"responseCode\":\"ServerDeviceFailure\",\"errorMessage\":\"bus timeout\",\"data\":null}",
                  SameGetResponse);

            Check(new SetModbusPayload(ModbusFunctionCode.WriteMultipleRegisters, 3, 40001, [0x00, 0x01, 0x00, 0x02]),
                  HwJsonContext.Default.SetModbusPayload,
                  "{\"functionCode\":\"WriteMultipleRegisters\",\"unitIdentifier\":3,\"address\":40001,\"data\":\"AAEAAg==\"}",
                  SameSetRequest);

            Check(new SetModbusResponsePayload(ModbusResponseCode.Ok, null), HwJsonContext.Default.SetModbusResponsePayload, "{\"responseCode\":\"Ok\",\"errorMessage\":null}");
        }

        private static void CheckEnums()
        {
            // Enum.GetValues<TEnum>() is the generic overload, which is not [RequiresDynamicCode]; enumerating every
            // member means a member added later is covered without editing this file.
            foreach (var functionCode in Enum.GetValues<ModbusFunctionCode>())
            {
                CheckEnumMember(functionCode, HwJsonContext.Default.ModbusFunctionCode);
            }

            foreach (var responseCode in Enum.GetValues<ModbusResponseCode>())
            {
                CheckEnumMember(responseCode, HwJsonContext.Default.ModbusResponseCode);
            }
        }

        private static void CheckEventsPayloads()
        {
            // The client-facing payload: a JsonNode value of every kind, written verbatim.
            Check(new PropertiesStatePayload([
                      new PropertyState("Setpoint", JsonNode.Parse("{\"Min\":5,\"max\":30.5}")),
                      new PropertyState("Power", JsonValue.Create(11.25)),
                      new PropertyState("Mode", JsonValue.Create("Eco")),
                      new PropertyState("Fault", null),
                  ]),
                  EventsJsonContext.Default.PropertiesStatePayload,
                  "{\"propertiesState\":[{\"propertyIdentifier\":\"Setpoint\",\"value\":{\"Min\":5,\"max\":30.5}},{\"propertyIdentifier\":\"Power\",\"value\":11.25},{\"propertyIdentifier\":\"Mode\",\"value\":\"Eco\"},{\"propertyIdentifier\":\"Fault\",\"value\":null}]}",
                  SamePropertiesState);

            // Enum-carrying payloads: the JsonStringEnumConverter<TEnum> instances listed on the context, not attributes
            // on the enum types, are what make these member names — a non-generic converter would only fail here.
            Check(new LogicConfigurationLoadFailedPayload(LogicConfigurationLoadFailureReason.CorruptConfig),
                  EventsJsonContext.Default.LogicConfigurationLoadFailedPayload,
                  "{\"reason\":\"CorruptConfig\"}");

            Check(new SystemServiceStatePayload("remote-console", "session-1", ServiceState.Started),
                  EventsJsonContext.Default.SystemServiceStatePayload,
                  "{\"serviceName\":\"remote-console\",\"instanceId\":\"session-1\",\"state\":\"Started\",\"information\":null}");

            Check(new BridgingDeclarationPayload([new TopicDeclaration("tenant/installation-b/#", AccessType.Publish)]),
                  EventsJsonContext.Default.BridgingDeclarationPayload,
                  "{\"declarations\":[{\"topicFilter\":\"tenant/installation-b/#\",\"access\":\"Publish\"}]}",
                  (left, right) => left.Declarations.SequenceEqual(right.Declarations));

            // An object-typed value: written by its runtime type, read back as a JsonElement and written again — both
            // need metadata the context registers explicitly.
            Check(new AlarmStateChangedPayload
                  {
                      AutomationId = new Guid("6f9619ff-8b86-d011-b42d-00c04fc964ff"),
                      Identifier = "overTemperature",
                      AlarmParameters =
                      [
                          new AlarmStateChangedPayload.AlarmParameter("temperature", 81.5),
                          new AlarmStateChangedPayload.AlarmParameter("active", true),
                      ],
                  },
                  EventsJsonContext.Default.AlarmStateChangedPayload,
                  "{\"automationId\":\"6f9619ff-8b86-d011-b42d-00c04fc964ff\",\"identifier\":\"overTemperature\",\"alarmParameters\":[{\"name\":\"temperature\",\"value\":81.5},{\"name\":\"active\",\"value\":true}]}",
                  (left, right) => left.AutomationId == right.AutomationId && left.Identifier == right.Identifier && left.AlarmParameters!.Select(parameter => parameter.Name)
                                       .SequenceEqual(right.AlarmParameters!.Select(parameter => parameter.Name)));
        }

        private static void CheckEnumMember<TEnum>(TEnum member, JsonTypeInfo<TEnum> typeInfo)
            where TEnum : struct, Enum
        {
            // The generic JsonStringEnumConverter<TEnum> on the enum type is what makes this the member name and not
            // its underlying number; that attribute is honoured by the context, which is the half of (a) worth proving.
            Check(member, typeInfo, $"\"{member}\"");
        }

        private static void Check<T>(T value, JsonTypeInfo<T> typeInfo, string expectedJson, Func<T, T, bool>? same = null)
        {
            var label = $"{typeof(T).Name} {expectedJson}";

            string json;
            try
            {
                json = JsonSerializer.Serialize(value, typeInfo);
            }
            catch (Exception exception)
            {
                Failures.Add($"{label}: serialise threw {exception.GetType().Name}: {exception.Message}");

                return;
            }

            if (json != expectedJson)
            {
                Failures.Add($"{label}: serialised to {json}");

                return;
            }

            T roundTripped;
            try
            {
                roundTripped = JsonSerializer.Deserialize(json, typeInfo)!;
            }
            catch (Exception exception)
            {
                Failures.Add($"{label}: deserialise threw {exception.GetType().Name}: {exception.Message}");

                return;
            }

            var equal = same is null ? EqualityComparer<T>.Default.Equals(roundTripped, value) : same(roundTripped, value);
            if (!equal)
            {
                Failures.Add($"{label}: round-tripped to a different value ({roundTripped})");
            }

            var reSerialised = JsonSerializer.Serialize(roundTripped, typeInfo);
            if (reSerialised != expectedJson)
            {
                Failures.Add($"{label}: re-serialised to {reSerialised}");
            }
        }

        // Records compare byte[] by reference, so the two data-carrying payloads need their vectors compared by value.
        private static bool SameGetResponse(GetModbusResponsePayload left, GetModbusResponsePayload right)
        {
            return left.ResponseCode == right.ResponseCode && left.ErrorMessage == right.ErrorMessage && SameBytes(left.Data, right.Data);
        }

        private static bool SameSetRequest(SetModbusPayload left, SetModbusPayload right)
        {
            return left.FunctionCode == right.FunctionCode && left.UnitIdentifier == right.UnitIdentifier && left.Address == right.Address && SameBytes(left.Data, right.Data);
        }

        // Records compare their List and JsonNode members by reference, so the states are compared by value. The values are
        // compared as JSON text: JsonNode.DeepEquals throws InsufficientExecutionStackException in the linux-musl-x64
        // NativeAOT binary, even on a flat object.
        private static bool SamePropertiesState(PropertiesStatePayload left, PropertiesStatePayload right)
        {
            if (left.PropertiesState.Count != right.PropertiesState.Count)
            {
                return false;
            }

            for (var index = 0; index < left.PropertiesState.Count; index++)
            {
                var leftState = left.PropertiesState[index];
                var rightState = right.PropertiesState[index];
                if (leftState.PropertyIdentifier != rightState.PropertyIdentifier || leftState.Value?.ToJsonString() != rightState.Value?.ToJsonString())
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SameBytes(byte[]? left, byte[]? right)
        {
            if (left is null || right is null)
            {
                return left is null && right is null;
            }

            if (left.Length != right.Length)
            {
                return false;
            }

            for (var index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
