using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vion.Contracts.Events;
using Vion.Contracts.Events.ServiceProviderToMesh;

namespace Vion.Contracts.Test.Events.ServiceProviderToMesh
{
    [TestClass]
    public class BridgingDeclarationPayloadShould
    {
        // Mirrors the platform wire convention: camelCase property names, and AccessType as its member name, as mesh's
        // BridgingJsonContext writes and reads it.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                        Converters = { new JsonStringEnumConverter<AccessType>() },
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
                                                             new("v1/test/5b1d0c9e-3a47-4f2b-8e6d-0c2a9f4b7d11/c84f2a60-9d1e-4b37-a5f2-6e3b8d0c1a92/dale/block-1/wind/remote/interface/send",
                                                                 AccessType.Publish),
                                                             new("v1/test/5b1d0c9e-3a47-4f2b-8e6d-0c2a9f4b7d11/0e7a3f19-62c4-4d8b-9b15-a4f0c6d2e873/dale/block-2/sun/remote/interface/send",
                                                                 AccessType.Publish),
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
