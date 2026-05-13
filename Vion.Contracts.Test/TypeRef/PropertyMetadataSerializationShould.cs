using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Vion.Contracts.TypeRef;

namespace Vion.Contracts.Test.TypeRef
{
    [TestClass]
    public class PropertyMetadataSerializationShould
    {
        [TestMethod]
        public void EmitNullForEmptyPresentationAndRuntime()
        {
            var meta = PropertyMetadata.Of(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.Double)));
            var json = meta.ToJson();
            Assert.IsNotNull(json["schema"]);
            Assert.IsNull(json["presentation"]);
            Assert.IsNull(json["runtime"]);
        }

        [TestMethod]
        public void EmitNonNullPresentationWhenAnyFieldIsSet()
        {
            var meta = new PropertyMetadata(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.Double)), new Presentation { Group = "Power" }, RuntimeMetadata.None);
            var json = meta.ToJson();
            Assert.IsNotNull(json["presentation"]);
            Assert.IsNull(json["runtime"]);
        }

        [TestMethod]
        public void EmitNonNullRuntimeWhenPersistentIsTrue()
        {
            var meta = new PropertyMetadata(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.Double)), Presentation.None, new RuntimeMetadata { Persistent = true });
            var json = meta.ToJson();
            Assert.IsNull(json["presentation"]);
            Assert.IsNotNull(json["runtime"]);
            Assert.IsTrue(json["runtime"]!["persistent"]!.GetValue<bool>());
        }

        [TestMethod]
        public void RoundtripFullPropertyMetadata()
        {
            var meta = new PropertyMetadata(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.Double)),
                                            new Presentation
                                            {
                                                DisplayName = "Voltage",
                                                Group = "Power",
                                                Order = 2,
                                                Category = "Configuration",
                                                Importance = "Primary",
                                                UIHint = "slider",
                                                Decimals = 3,
                                                StatusMappings = ImmutableDictionary<string, string>.Empty.Add("Ok", "ok").Add("Warning", "warning"),
                                                EnumLabels = ImmutableDictionary<string, string>.Empty.Add("Ok", "OK").Add("Warning", "Warnung"),
                                            },
                                            new RuntimeMetadata { Persistent = true });
            var roundtripped = PropertyMetadataSerialization.FromJson(meta.ToJson());
            Assert.AreEqual(meta, roundtripped);
        }

        [TestMethod]
        public void SerializeEnumLabelsAsObjectMap()
        {
            var meta = new PropertyMetadata(TypeSchema.Of(new EnumTypeRef("AlarmState", new[] { "Ok", "Warning", "Critical" }.ToImmutableArray())),
                                            new Presentation
                                            {
                                                EnumLabels = ImmutableDictionary<string, string>.Empty
                                                                                                .Add("Ok", "Alles in Ordnung")
                                                                                                .Add("Warning", "Warnung")
                                                                                                .Add("Critical", "Kritisch"),
                                            },
                                            RuntimeMetadata.None);
            var json = meta.ToJson();
            var labels = json["presentation"]!["enumLabels"]!.AsObject();
            Assert.AreEqual("Alles in Ordnung", labels["Ok"]!.GetValue<string>());
            Assert.AreEqual("Warnung", labels["Warning"]!.GetValue<string>());
            Assert.AreEqual("Kritisch", labels["Critical"]!.GetValue<string>());
        }

        [TestMethod]
        public void RoundtripWithEmptyPresentationAndRuntime()
        {
            var meta = PropertyMetadata.Of(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.Double)));
            var roundtripped = PropertyMetadataSerialization.FromJson(meta.ToJson());
            Assert.AreEqual(meta, roundtripped);
        }

        [TestMethod]
        public void RejectMissingSchemaField()
        {
            var json = JsonNode.Parse("{\"presentation\":null,\"runtime\":null}")!;
            Assert.Throws<InvalidSchemaException>(() => PropertyMetadataSerialization.FromJson(json));
        }

        [TestMethod]
        public void RoundtripPresentationOrder()
        {
            var metadata = new PropertyMetadata(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.Double)), new Presentation { Order = -1 }, RuntimeMetadata.None);
            var json = metadata.ToJson();
            var roundtripped = PropertyMetadataSerialization.FromJson(json);
            Assert.AreEqual(-1, roundtripped.Presentation.Order);
        }

        [TestMethod]
        public void OmitOrderKeyWhenNull()
        {
            var metadata = new PropertyMetadata(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.Double)), new Presentation { Group = "Power" }, RuntimeMetadata.None);
            var json = metadata.ToJson();

            // Presentation is non-empty (Group is set), so it serialises as an object — assert that
            // object has no `order` key.
            Assert.IsFalse(json["presentation"]!.AsObject().ContainsKey("order"));
        }
    }
}
