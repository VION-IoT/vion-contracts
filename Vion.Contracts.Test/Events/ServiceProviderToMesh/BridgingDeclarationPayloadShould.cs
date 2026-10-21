using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Vion.Contracts.Events;
using Vion.Contracts.Events.ServiceProviderToMesh;

namespace Vion.Contracts.Test.Events.ServiceProviderToMesh
{
    [TestClass]
    public class BridgingDeclarationPayloadShould
    {
        // Mirrors the platform wire convention: camelCase property names.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        // Local hop only: mesh never forwards it to the cloud, so it is no [Schema] / IMessage envelope.
        [TestMethod]
        public void NotCarryASchemaAttribute()
        {
            Assert.IsNull(typeof(BridgingDeclarationPayload).GetCustomAttribute<SchemaAttribute>());
        }

        [TestMethod]
        public void NotBeAMessage()
        {
            Assert.IsFalse(typeof(IMessage).IsAssignableFrom(typeof(BridgingDeclarationPayload)));
        }

        [TestMethod]
        public void RoundTripEveryDeclaration()
        {
            var payload = new BridgingDeclarationPayload(new List<TopicDeclaration>
                                                         {
                                                             new("tenant/installation-b/sp-1/sw/property/set/#", AccessType.Publish),
                                                             new("tenant/installation-c/+/sw/property/set/foo", AccessType.Publish),
                                                         });

            var roundtripped = JsonSerializer.Deserialize<BridgingDeclarationPayload>(JsonSerializer.Serialize(payload, WireOptions), WireOptions);

            CollectionAssert.AreEqual(payload.Declarations, roundtripped.Declarations);
        }

        [TestMethod]
        public void RoundTripAnEmptyDeclarationList()
        {
            var payload = new BridgingDeclarationPayload(new List<TopicDeclaration>());

            var roundtripped = JsonSerializer.Deserialize<BridgingDeclarationPayload>(JsonSerializer.Serialize(payload, WireOptions), WireOptions);

            Assert.IsNotNull(roundtripped.Declarations);
            Assert.IsEmpty(roundtripped.Declarations);
        }
    }
}
