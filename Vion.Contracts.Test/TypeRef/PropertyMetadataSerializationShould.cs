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

        [TestMethod]
        public void RoundtripPresentationFormat()
        {
            var metadata = new PropertyMetadata(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.String)), new Presentation { Format = "LLLL" }, RuntimeMetadata.None);
            var json = metadata.ToJson();
            var roundtripped = PropertyMetadataSerialization.FromJson(json);
            Assert.AreEqual("LLLL", roundtripped.Presentation.Format);
        }

        [TestMethod]
        public void EmitFormatKeyVerbatim()
        {
            // The codec round-trips the value as-is — it doesn't validate moment-token syntax.
            // That responsibility lives at the renderer (dashboard / DevHost).
            var metadata = new PropertyMetadata(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.String)),
                                                new Presentation { Format = "YYYY-MM-DD HH:mm:ss.SSS" },
                                                RuntimeMetadata.None);
            var json = metadata.ToJson();
            Assert.AreEqual("YYYY-MM-DD HH:mm:ss.SSS", json["presentation"]!["format"]!.GetValue<string>());
        }

        [TestMethod]
        public void EmitFormatKeyForRelativeSentinel()
        {
            // "relative" is a reserved sentinel; the codec doesn't treat it specially —
            // it round-trips verbatim and the renderer interprets it.
            var metadata = new PropertyMetadata(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.String)), new Presentation { Format = "relative" }, RuntimeMetadata.None);
            var json = metadata.ToJson();
            Assert.AreEqual("relative", json["presentation"]!["format"]!.GetValue<string>());
        }

        [TestMethod]
        public void OmitFormatKeyWhenNull()
        {
            var metadata = new PropertyMetadata(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.Double)), new Presentation { Group = "Power" }, RuntimeMetadata.None);
            var json = metadata.ToJson();
            Assert.IsFalse(json["presentation"]!.AsObject().ContainsKey("format"));
        }

        [TestMethod]
        public void EmitNonNullRuntimeWhenThrottleIsSetEvenWithoutPersistent()
        {
            var meta = new PropertyMetadata(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.Double)),
                                            Presentation.None,
                                            new RuntimeMetadata { Throttle = new ThrottleMetadata { MinInterval = "1s", MinChange = "0.1" } });
            Assert.IsFalse(meta.Runtime.IsEmpty);
            var json = meta.ToJson();
            Assert.IsNotNull(json["runtime"]);
            // Not persistent -> no `persistent` key.
            Assert.IsFalse(json["runtime"]!.AsObject().ContainsKey("persistent"));
            Assert.AreEqual("1s", json["runtime"]!["throttle"]!["minInterval"]!.GetValue<string>());
            Assert.AreEqual("0.1", json["runtime"]!["throttle"]!["minChange"]!.GetValue<string>());
        }

        [TestMethod]
        public void OmitMinChangeAndImmediateKeysWhenAtTheirDefaults()
        {
            var meta = new PropertyMetadata(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.Double)),
                                            Presentation.None,
                                            new RuntimeMetadata { Throttle = new ThrottleMetadata { MinInterval = "250ms" } });
            var throttle = meta.ToJson()["runtime"]!["throttle"]!.AsObject();
            Assert.AreEqual("250ms", throttle["minInterval"]!.GetValue<string>());
            Assert.IsFalse(throttle.ContainsKey("minChange"));
            Assert.IsFalse(throttle.ContainsKey("immediate"));
        }

        [TestMethod]
        public void EmitImmediateKeyOnlyWhenTrue()
        {
            var meta = new PropertyMetadata(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.Double)),
                                            Presentation.None,
                                            new RuntimeMetadata { Throttle = new ThrottleMetadata { MinInterval = "250ms", Immediate = true } });
            Assert.IsTrue(meta.ToJson()["runtime"]!["throttle"]!["immediate"]!.GetValue<bool>());
        }

        [TestMethod]
        public void RoundtripThrottleMetadata()
        {
            var meta = new PropertyMetadata(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.Double)),
                                            Presentation.None,
                                            new RuntimeMetadata { Throttle = new ThrottleMetadata { MinInterval = "2s", MinChange = "5", Immediate = false } });
            var roundtripped = PropertyMetadataSerialization.FromJson(meta.ToJson());
            Assert.AreEqual(meta, roundtripped);
            Assert.AreEqual("2s", roundtripped.Runtime.Throttle!.MinInterval);
            Assert.AreEqual("5", roundtripped.Runtime.Throttle!.MinChange);
        }

        [TestMethod]
        public void RoundtripPersistentAndThrottleTogether()
        {
            var meta = new PropertyMetadata(TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.Double)),
                                            Presentation.None,
                                            new RuntimeMetadata { Persistent = true, Throttle = new ThrottleMetadata { MinInterval = "0", Immediate = true } });
            var json = meta.ToJson();
            Assert.IsTrue(json["runtime"]!["persistent"]!.GetValue<bool>());
            Assert.AreEqual(meta, PropertyMetadataSerialization.FromJson(json));
        }
    }
}
