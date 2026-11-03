using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using Vion.Contracts.Events;
using Vion.Contracts.Events.CloudToMesh;
using Vion.Contracts.Events.CloudToMesh.Automations;
using Vion.Contracts.Events.MeshToCloud;
using Vion.Contracts.Events.ServiceProviderToMesh;

namespace Vion.Contracts.Test.Events
{
    /// <summary>
    ///     Pins <see cref="EventsJsonContext" />'s options against the ones mesh serializes these payloads with today.
    ///     This repository cannot reference mesh, so the check is a reconciliation: the expected values below were read
    ///     off every <c>*JsonContext.cs</c> in mesh (at <c>origin/main</c> <c>1958a043</c>, 15 contexts), and a change on
    ///     either side has to be reconciled by hand.
    /// </summary>
    [TestClass]
    public class EventsJsonContextShould
    {
        // Every JsonStringEnumConverter<T> registered across mesh's contexts (18 enums), plus AccessType, which no mesh
        // context converts because nothing produces or reads BridgingDeclarationPayload as JSON yet.
        private static readonly Type[] StringEnums =
        [
            typeof(ActionType),
            typeof(AutomationType),
            typeof(ConnectionStatus),
            typeof(HealthStatus),
            typeof(LogLevel),
            typeof(LogicConfigurationLoadFailureReason),
            typeof(ParameterType),
            typeof(RegistrationStatus),
            typeof(ScheduleType),
            typeof(ServiceState),
            typeof(SeverityLevel),
            typeof(SyncStatus),
            typeof(SystemEventSource),
            typeof(SystemEventStatus),
            typeof(SystemEventType),
            typeof(TimingType),
            typeof(TimingUnit),
            typeof(TriggerType),
            typeof(AccessType),
        ];

        [TestMethod]
        public void UseCamelCaseForPropertiesAndDictionaryKeysAsEveryMeshContextDoes()
        {
            Assert.AreEqual(JsonNamingPolicy.CamelCase, EventsJsonContext.Default.Options.PropertyNamingPolicy);
            Assert.AreEqual(JsonNamingPolicy.CamelCase, EventsJsonContext.Default.Options.DictionaryKeyPolicy);
        }

        [TestMethod]
        public void LeaveEveryOtherOptionAtItsDefaultAsEveryMeshContextDoes()
        {
            var options = EventsJsonContext.Default.Options;
            var defaults = new JsonSerializerOptions();

            Assert.AreEqual(defaults.DefaultIgnoreCondition, options.DefaultIgnoreCondition);
            Assert.AreEqual(defaults.NumberHandling, options.NumberHandling);
            Assert.AreEqual(defaults.PropertyNameCaseInsensitive, options.PropertyNameCaseInsensitive);
            Assert.AreEqual(defaults.ReadCommentHandling, options.ReadCommentHandling);
            Assert.AreEqual(defaults.AllowTrailingCommas, options.AllowTrailingCommas);
            Assert.AreEqual(defaults.UnmappedMemberHandling, options.UnmappedMemberHandling);
            Assert.AreEqual(defaults.RespectNullableAnnotations, options.RespectNullableAnnotations);
            Assert.AreEqual(defaults.RespectRequiredConstructorParameters, options.RespectRequiredConstructorParameters);
            Assert.AreEqual(defaults.WriteIndented, options.WriteIndented);
        }

        [TestMethod]
        public void ConvertExactlyTheReconciledEnumsAsStrings()
        {
            CollectionAssert.AreEquivalent(StringEnums, ConvertedEnums().ToList());
        }

        [TestMethod]
        public void ConvertEveryEnumAPayloadCarries()
        {
            // Walks the generated metadata from every registered payload, so an enum added to a payload later fails here
            // until its converter is listed — instead of going out as a number.
            var carried = CarriedEnums();

            var unconverted = carried.Except(ConvertedEnums()).Select(type => type.FullName).ToList();
            var unused = ConvertedEnums().Except(carried).Select(type => type.FullName).ToList();

            Assert.IsEmpty(unconverted, $"Add a JsonStringEnumConverter<T> to {nameof(EventsJsonContext)} for: {string.Join(", ", unconverted)}");
            Assert.IsEmpty(unused, $"No payload carries: {string.Join(", ", unused)}");
        }

        [TestMethod]
        public void WriteAnEnumAsItsMemberName()
        {
            Assert.AreEqual("{\"reason\":\"MissingConfig\"}",
                            JsonSerializer.Serialize(new LogicConfigurationLoadFailedPayload(LogicConfigurationLoadFailureReason.MissingConfig),
                                                     EventsJsonContext.Default.LogicConfigurationLoadFailedPayload));
            Assert.AreEqual("{\"logLevel\":\"Warning\"}", JsonSerializer.Serialize(new SetLogLevelPayload(LogLevel.Warning), EventsJsonContext.Default.SetLogLevelPayload));
            Assert.AreEqual("{\"declarations\":[{\"topicFilter\":\"tenant/installation-b/#\",\"access\":\"Publish\"}]}",
                            JsonSerializer.Serialize(new BridgingDeclarationPayload([new TopicDeclaration("tenant/installation-b/#", AccessType.Publish)]),
                                                     EventsJsonContext.Default.BridgingDeclarationPayload));
        }

        [TestMethod]
        public void WriteAnObjectTypedAlarmParameterThroughTheContext()
        {
            // AlarmParameter.Value is object: the context needs metadata for its runtime type, as mesh's AlarmJsonContext has.
            var payload = new AlarmStateChangedPayload
                          {
                              AutomationId = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff"),
                              Identifier = "overTemperature",
                              AlarmParameters =
                              [
                                  new AlarmStateChangedPayload.AlarmParameter("temperature", 81.5),
                                  new AlarmStateChangedPayload.AlarmParameter("active", true),
                              ],
                          };

            Assert.AreEqual("{\"automationId\":\"6f9619ff-8b86-d011-b42d-00c04fc964ff\",\"identifier\":\"overTemperature\",\"alarmParameters\":[{\"name\":\"temperature\",\"value\":81.5},{\"name\":\"active\",\"value\":true}]}",
                            JsonSerializer.Serialize(payload, EventsJsonContext.Default.AlarmStateChangedPayload));
        }

        [TestMethod]
        public void RewriteAnObjectTypedAlarmParameterItHasRead()
        {
            // Read back, an object value is a JsonElement; writing it again needs JsonElement metadata in the context.
            const string json = "{\"automationId\":\"6f9619ff-8b86-d011-b42d-00c04fc964ff\",\"identifier\":\"x\",\"alarmParameters\":[{\"name\":\"t\",\"value\":81.5}]}";
            var read = JsonSerializer.Deserialize(json, EventsJsonContext.Default.AlarmStateChangedPayload)!;
            Assert.AreEqual(json, JsonSerializer.Serialize(read, EventsJsonContext.Default.AlarmStateChangedPayload));
        }

        private static IEnumerable<Type> ConvertedEnums()
        {
            return EventsJsonContext.Default
                                    .Options
                                    .Converters
                                    .Select(converter => converter.GetType())
                                    .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(JsonStringEnumConverter<>))
                                    .Select(type => type.GetGenericArguments()[0]);
        }

        private static HashSet<Type> CarriedEnums()
        {
            var enums = new HashSet<Type>();
            var visited = new HashSet<Type>();
            var pending = new Stack<Type>(EventsSchemaExporter.PayloadTypes);

            while (pending.Count > 0)
            {
                var type = pending.Pop();
                type = Nullable.GetUnderlyingType(type) ?? type;
                if (!visited.Add(type))
                {
                    continue;
                }

                if (type.IsEnum)
                {
                    enums.Add(type);

                    continue;
                }

                var info = EventsJsonContext.Default.GetTypeInfo(type);
                if (info is null)
                {
                    continue;
                }

                switch (info.Kind)
                {
                    case JsonTypeInfoKind.Object:
                        foreach (var property in info.Properties)
                        {
                            pending.Push(property.PropertyType);
                        }

                        break;
                    case JsonTypeInfoKind.Enumerable:
                        pending.Push(info.ElementType!);

                        break;
                    case JsonTypeInfoKind.Dictionary:
                        pending.Push(info.KeyType!);
                        pending.Push(info.ElementType!);

                        break;
                }
            }

            return enums;
        }
    }
}
