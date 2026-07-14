using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Vion.Contracts.Events;
using Vion.Contracts.Events.CloudToMesh;
using Vion.Contracts.Events.MeshToCloud;

namespace Vion.Contracts.Test.Events.CloudToMesh
{
    [TestClass]
    public class LogicConfigurationRetrievedPayloadShould
    {
        // Mirrors the platform wire convention: camelCase property names.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void CarryTheWireSchemaNameThatDrivesDispatch()
        {
            var schema = typeof(LogicConfigurationRetrievedPayload).GetCustomAttribute<SchemaAttribute>();

            Assert.IsNotNull(schema, "payload must carry a [Schema] attribute");
            Assert.AreEqual("LogicConfigurationRetrievedPayload", schema.Schema);
        }

        [TestMethod]
        public void RoundTripTheConfigBodyAndItsSyncStatus()
        {
            var syncStatusId = Guid.Parse("0195f0d1-1111-7abc-8def-000000000001");
            var config = new LogicConfiguration
                         {
                             LogicBlockInstances =
                             [
                                 new LogicConfiguration.LogicBlockInstance
                                 {
                                     Id = "b1",
                                     PackageId = "pkg",
                                     PackageVersion = "4.0.0",
                                     TypeFullName = "Ns.X",
                                     Name = "n",
                                     Services = [],
                                 },
                             ],
                             InterfaceMappings = [],
                             ContractMappings = [],
                         };
            var payload = new LogicConfigurationRetrievedPayload(config, new ObjectSyncStatus(syncStatusId, SyncStatus.Completed));

            var roundtripped = JsonSerializer.Deserialize<LogicConfigurationRetrievedPayload>(JsonSerializer.Serialize(payload, WireOptions), WireOptions)!;

            Assert.IsNotNull(roundtripped.Config);
            Assert.AreEqual("b1", roundtripped.Config!.LogicBlockInstances.Single().Id);
            Assert.IsNotNull(roundtripped.SyncStatus);
            Assert.AreEqual(syncStatusId, roundtripped.SyncStatus!.Value.SyncStatusId);
            Assert.AreEqual(SyncStatus.Completed, roundtripped.SyncStatus!.Value.Status);
        }

        [TestMethod]
        public void OmitTheConfigBodyWhenUpToDateButStillCarryTheSyncStatus()
        {
            var payload = new LogicConfigurationRetrievedPayload(null, new ObjectSyncStatus(Guid.Parse("0195f0d1-2222-7abc-8def-000000000002"), SyncStatus.Completed));

            var roundtripped = JsonSerializer.Deserialize<LogicConfigurationRetrievedPayload>(JsonSerializer.Serialize(payload, WireOptions), WireOptions)!;

            Assert.IsNull(roundtripped.Config);
            Assert.IsNotNull(roundtripped.SyncStatus);
        }
    }
}
