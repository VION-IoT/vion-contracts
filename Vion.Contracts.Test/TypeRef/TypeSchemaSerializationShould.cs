using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Vion.Contracts.TypeRef;

namespace Vion.Contracts.Test.TypeRef
{
    [TestClass]
    public class TypeSchemaSerializationShould
    {
        [TestMethod]
        [DataRow(PrimitiveKind.Bool, "{\"type\":\"boolean\"}")]
        [DataRow(PrimitiveKind.String, "{\"type\":\"string\"}")]
        [DataRow(PrimitiveKind.Byte, "{\"type\":\"integer\",\"format\":\"uint8\"}")]
        [DataRow(PrimitiveKind.Short, "{\"type\":\"integer\",\"format\":\"int16\"}")]
        [DataRow(PrimitiveKind.UShort, "{\"type\":\"integer\",\"format\":\"uint16\"}")]
        [DataRow(PrimitiveKind.Int, "{\"type\":\"integer\",\"format\":\"int32\"}")]
        [DataRow(PrimitiveKind.UInt, "{\"type\":\"integer\",\"format\":\"uint32\"}")]
        [DataRow(PrimitiveKind.Long, "{\"type\":\"integer\",\"format\":\"int64\"}")]
        [DataRow(PrimitiveKind.Float, "{\"type\":\"number\",\"format\":\"float\"}")]
        [DataRow(PrimitiveKind.Double, "{\"type\":\"number\",\"format\":\"double\"}")]
        [DataRow(PrimitiveKind.DateTime, "{\"type\":\"string\",\"format\":\"date-time\"}")]
        [DataRow(PrimitiveKind.Duration, "{\"type\":\"string\",\"format\":\"duration\"}")]
        public void EmitJsonSchemaForPrimitiveKind(PrimitiveKind kind, string expected)
        {
            var schema = TypeSchema.Of(new PrimitiveTypeRef(kind));
            var actual = schema.ToJsonSchema();

            // Compare via canonical-JSON string form (JsonNode.Equals is reference-based).
            var expectedCanonical = JsonNode.Parse(expected)!.ToJsonString();
            Assert.AreEqual(expectedCanonical, actual.ToJsonString());
        }

        [TestMethod]
        public void EmitJsonSchemaForEnumWithMultipleMembers()
        {
            var schema = TypeSchema.Of(new EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical")));
            var actual = schema.ToJsonSchema().ToJsonString();

            var expected = "{\"type\":\"string\",\"title\":\"AlarmState\",\"enum\":[\"Ok\",\"Warning\",\"Critical\"]}";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EmitJsonSchemaForEnumWithSingleMember()
        {
            var schema = TypeSchema.Of(new EnumTypeRef("Mode", ImmutableArray.Create("Auto")));
            var actual = schema.ToJsonSchema().ToJsonString();

            var expected = "{\"type\":\"string\",\"title\":\"Mode\",\"enum\":[\"Auto\"]}";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void PreserveEnumMemberDeclarationOrderInJsonSchema()
        {
            // Same members, different declaration order — JSON output must reflect the input order.
            var ascending = TypeSchema.Of(new EnumTypeRef("Severity", ImmutableArray.Create("Low", "Medium", "High")));
            var descending = TypeSchema.Of(new EnumTypeRef("Severity", ImmutableArray.Create("High", "Medium", "Low")));

            var ascJson = ascending.ToJsonSchema().ToJsonString();
            var descJson = descending.ToJsonSchema().ToJsonString();

            Assert.AreEqual("{\"type\":\"string\",\"title\":\"Severity\",\"enum\":[\"Low\",\"Medium\",\"High\"]}", ascJson);
            Assert.AreEqual("{\"type\":\"string\",\"title\":\"Severity\",\"enum\":[\"High\",\"Medium\",\"Low\"]}", descJson);
            Assert.AreNotEqual(ascJson, descJson);
        }

        [TestMethod]
        public void EmitJsonSchemaForStructWithPrimitiveFields()
        {
            var s = new StructTypeRef("Coordinates",
                                      ImmutableArray.Create(new StructField("lat", new PrimitiveTypeRef(PrimitiveKind.Double)),
                                                            new StructField("lon", new PrimitiveTypeRef(PrimitiveKind.Double))),
                                      ImmutableArray.Create("lat", "lon"));
            var schema = TypeSchema.Of(s);
            var actual = schema.ToJsonSchema().ToJsonString();

            var expected = "{\"type\":\"object\"," + "\"title\":\"Coordinates\"," + "\"properties\":{" + "\"lat\":{\"type\":\"number\",\"format\":\"double\"}," +
                           "\"lon\":{\"type\":\"number\",\"format\":\"double\"}" + "}," + "\"required\":[\"lat\",\"lon\"]," + "\"additionalProperties\":false}";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void PreserveStructFieldDeclarationOrderInJsonSchema()
        {
            var ascending = new StructTypeRef("Pair",
                                              ImmutableArray.Create(new StructField("a", new PrimitiveTypeRef(PrimitiveKind.Int)),
                                                                    new StructField("b", new PrimitiveTypeRef(PrimitiveKind.Int))),
                                              ImmutableArray.Create("a", "b"));
            var descending = new StructTypeRef("Pair",
                                               ImmutableArray.Create(new StructField("b", new PrimitiveTypeRef(PrimitiveKind.Int)),
                                                                     new StructField("a", new PrimitiveTypeRef(PrimitiveKind.Int))),
                                               ImmutableArray.Create("b", "a"));

            var ascJson = TypeSchema.Of(ascending).ToJsonSchema().ToJsonString();
            var descJson = TypeSchema.Of(descending).ToJsonSchema().ToJsonString();

            // Properties object key order, plus required-array element order, both differ.
            Assert.AreNotEqual(ascJson, descJson);
            StringAssert.Contains(ascJson, "\"properties\":{\"a\":{");
            StringAssert.Contains(descJson, "\"properties\":{\"b\":{");
            StringAssert.Contains(ascJson, "\"required\":[\"a\",\"b\"]");
            StringAssert.Contains(descJson, "\"required\":[\"b\",\"a\"]");
        }

        [TestMethod]
        public void EmitRequiredArraySubsetWhenSomeFieldsOptional()
        {
            var s = new StructTypeRef("PartialPoint",
                                      ImmutableArray.Create(new StructField("x", new PrimitiveTypeRef(PrimitiveKind.Double)),
                                                            new StructField("y", new PrimitiveTypeRef(PrimitiveKind.Double)),
                                                            new StructField("z", new PrimitiveTypeRef(PrimitiveKind.Double))),
                                      ImmutableArray.Create("x", "y")); // z is optional
            var actual = TypeSchema.Of(s).ToJsonSchema().ToJsonString();

            StringAssert.Contains(actual, "\"properties\":{\"x\":{");
            StringAssert.Contains(actual, "\"y\":{");
            StringAssert.Contains(actual, "\"z\":{");
            StringAssert.Contains(actual, "\"required\":[\"x\",\"y\"]");
            Assert.DoesNotContain(actual, "\"required\":[\"x\",\"y\",\"z\"]");
        }

        [TestMethod]
        public void EmitJsonSchemaForArrayOfPrimitive()
        {
            var schema = TypeSchema.Of(new ArrayTypeRef(new PrimitiveTypeRef(PrimitiveKind.Double)));
            var actual = schema.ToJsonSchema().ToJsonString();
            var expected = "{\"type\":\"array\",\"items\":{\"type\":\"number\",\"format\":\"double\"}}";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EmitJsonSchemaForArrayOfEnum()
        {
            var schema = TypeSchema.Of(new ArrayTypeRef(new EnumTypeRef("Severity", ImmutableArray.Create("Low", "High"))));
            var actual = schema.ToJsonSchema().ToJsonString();
            var expected = "{\"type\":\"array\",\"items\":{\"type\":\"string\",\"title\":\"Severity\",\"enum\":[\"Low\",\"High\"]}}";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EmitJsonSchemaForArrayOfStruct()
        {
            var struc = new StructTypeRef("Coordinates", ImmutableArray.Create(new StructField("lat", new PrimitiveTypeRef(PrimitiveKind.Double))), ImmutableArray.Create("lat"));
            var schema = TypeSchema.Of(new ArrayTypeRef(struc));
            var actual = schema.ToJsonSchema().ToJsonString();

            StringAssert.Contains(actual, "\"type\":\"array\"");
            StringAssert.Contains(actual, "\"items\":{\"type\":\"object\"");
            StringAssert.Contains(actual, "\"title\":\"Coordinates\"");
        }

        [TestMethod]
        public void EmitJsonSchemaForNullablePrimitive()
        {
            var schema = TypeSchema.Of(new NullableTypeRef(new PrimitiveTypeRef(PrimitiveKind.Double)));
            var actual = schema.ToJsonSchema().ToJsonString();
            var expected = "{\"type\":[\"number\",\"null\"],\"format\":\"double\"}";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EmitJsonSchemaForNullableEnumWithNullAppendedToEnumArray()
        {
            var schema = TypeSchema.Of(new NullableTypeRef(new EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning"))));
            var actual = schema.ToJsonSchema().ToJsonString();
            var expected = "{\"type\":[\"string\",\"null\"],\"title\":\"AlarmState\",\"enum\":[\"Ok\",\"Warning\",null]}";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EmitJsonSchemaForNullableStructWithTypeArrayWidened()
        {
            var struc = new StructTypeRef("Coordinates", ImmutableArray.Create(new StructField("lat", new PrimitiveTypeRef(PrimitiveKind.Double))), ImmutableArray.Create("lat"));
            var schema = TypeSchema.Of(new NullableTypeRef(struc));
            var actual = schema.ToJsonSchema().ToJsonString();

            StringAssert.Contains(actual, "\"type\":[\"object\",\"null\"]");
            StringAssert.Contains(actual, "\"title\":\"Coordinates\"");
            StringAssert.Contains(actual, "\"additionalProperties\":false");
        }

        [TestMethod]
        public void EmitJsonSchemaForArrayOfNullableStruct()
        {
            var struc = new StructTypeRef("Coordinates",
                                          ImmutableArray.Create(new StructField("lat", new PrimitiveTypeRef(PrimitiveKind.Double)),
                                                                new StructField("lon", new PrimitiveTypeRef(PrimitiveKind.Double))),
                                          ImmutableArray.Create("lat", "lon"));
            var schema = TypeSchema.Of(new ArrayTypeRef(new NullableTypeRef(struc)));
            var actual = schema.ToJsonSchema().ToJsonString();

            // From spec §5.1's worked example.
            StringAssert.Contains(actual, "\"type\":\"array\"");
            StringAssert.Contains(actual, "\"type\":[\"object\",\"null\"]");
            StringAssert.Contains(actual, "\"title\":\"Coordinates\"");
        }

        [TestMethod]
        public void ApplyTitleDescriptionAndConstraintsToPrimitiveSchema()
        {
            var schema = new TypeSchema(new PrimitiveTypeRef(PrimitiveKind.Double),
                                        new TypeAnnotations { Title = "Voltage", Description = "Setpoint", Unit = "V", Minimum = 0, Maximum = 250 },
                                        ImmutableDictionary<string, TypeAnnotations>.Empty);
            var actual = schema.ToJsonSchema().ToJsonString();

            // Expected key order: type, format, title, description, minimum, maximum, x-unit
            var expected = "{\"type\":\"number\",\"format\":\"double\"," + "\"title\":\"Voltage\",\"description\":\"Setpoint\"," +
                           "\"minimum\":0,\"maximum\":250,\"x-unit\":\"V\"}";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EmitReadOnlyKeyOnlyWhenAnnotationIsTrue()
        {
            var off = new TypeSchema(new PrimitiveTypeRef(PrimitiveKind.Double), new TypeAnnotations { ReadOnly = false }, ImmutableDictionary<string, TypeAnnotations>.Empty);
            var on = new TypeSchema(new PrimitiveTypeRef(PrimitiveKind.Double), new TypeAnnotations { ReadOnly = true }, ImmutableDictionary<string, TypeAnnotations>.Empty);

            Assert.DoesNotContain(off.ToJsonSchema().ToJsonString(), "readOnly");
            StringAssert.Contains(on.ToJsonSchema().ToJsonString(), "\"readOnly\":true");
        }

        [TestMethod]
        public void EmitNoAnnotationKeysWhenAnnotationsAreNone()
        {
            var schema = TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.Double));
            var actual = schema.ToJsonSchema().ToJsonString();

            // Only the type/format keys from BuildPrimitive — no annotation keys.
            Assert.AreEqual("{\"type\":\"number\",\"format\":\"double\"}", actual);
        }

        [TestMethod]
        public void DoNotOverwriteIdentityBearingEnumTitle()
        {
            // Enum's Title is identity-bearing on EnumTypeRef and set by BuildEnum.
            // Even if a property-level annotation provides a different Title, the
            // identity-bearing one must win.
            var schema = new TypeSchema(new EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning")),
                                        new TypeAnnotations { Title = "PropertyLevelTitle" },
                                        ImmutableDictionary<string, TypeAnnotations>.Empty);
            var actual = schema.ToJsonSchema().ToJsonString();

            StringAssert.Contains(actual, "\"title\":\"AlarmState\"");
            Assert.DoesNotContain(actual, "\"title\":\"PropertyLevelTitle\"");
        }

        [TestMethod]
        public void DoNotOverwriteIdentityBearingStructTitle()
        {
            var struc = new StructTypeRef("Coordinates", ImmutableArray.Create(new StructField("lat", new PrimitiveTypeRef(PrimitiveKind.Double))), ImmutableArray.Create("lat"));
            var schema = new TypeSchema(struc, new TypeAnnotations { Title = "PropertyLevelTitle" }, ImmutableDictionary<string, TypeAnnotations>.Empty);
            var actual = schema.ToJsonSchema().ToJsonString();

            StringAssert.Contains(actual, "\"title\":\"Coordinates\"");
            Assert.DoesNotContain(actual, "\"title\":\"PropertyLevelTitle\"");
        }

        [TestMethod]
        public void ApplyPerStructFieldAnnotationsToSubschemas()
        {
            // The spec §5.1 worked example for Coordinates3D: each struct field has its own annotations.
            var s = new StructTypeRef("Coordinates3D",
                                      ImmutableArray.Create(new StructField("lat", new PrimitiveTypeRef(PrimitiveKind.Double)),
                                                            new StructField("lon", new PrimitiveTypeRef(PrimitiveKind.Double)),
                                                            new StructField("altitude", new PrimitiveTypeRef(PrimitiveKind.Double))),
                                      ImmutableArray.Create("lat", "lon", "altitude"));
            var sfa = ImmutableDictionary<string, TypeAnnotations>.Empty
                                                                  .Add("lat", new TypeAnnotations { Unit = "deg", Minimum = -90, Maximum = 90 })
                                                                  .Add("lon", new TypeAnnotations { Unit = "deg", Minimum = -180, Maximum = 180 })
                                                                  .Add("altitude", new TypeAnnotations { Unit = "m" });
            var schema = new TypeSchema(s, TypeAnnotations.None, sfa);
            var actual = schema.ToJsonSchema().ToJsonString();

            // Each subschema should carry its own annotations.
            StringAssert.Contains(actual, "\"lat\":{\"type\":\"number\",\"format\":\"double\",\"minimum\":-90,\"maximum\":90,\"x-unit\":\"deg\"}");
            StringAssert.Contains(actual, "\"lon\":{\"type\":\"number\",\"format\":\"double\",\"minimum\":-180,\"maximum\":180,\"x-unit\":\"deg\"}");
            StringAssert.Contains(actual, "\"altitude\":{\"type\":\"number\",\"format\":\"double\",\"x-unit\":\"m\"}");
        }

        [TestMethod]
        public void ApplyAnnotationsToBothArrayAndItemsForArrayProperty()
        {
            // Per spec §5.1 array convention: x-unit etc. propagate to both `items` and the array node
            // (via BuildArray passing annotations through to the recursive BuildSchema, plus the outer
            // ApplyAnnotations call).
            var schema = new TypeSchema(new ArrayTypeRef(new PrimitiveTypeRef(PrimitiveKind.Double)),
                                        new TypeAnnotations { Unit = "V" },
                                        ImmutableDictionary<string, TypeAnnotations>.Empty);
            var actual = schema.ToJsonSchema().ToJsonString();

            // x-unit on items
            StringAssert.Contains(actual, "\"items\":{\"type\":\"number\",\"format\":\"double\",\"x-unit\":\"V\"}");

            // x-unit on the array node itself (after items)
            StringAssert.Contains(actual, "\"x-unit\":\"V\"}");
        }

        [TestMethod]
        public void DoNotApplyOuterAnnotationsTwiceOnNullablePrimitive()
        {
            // BuildNullable passes empty annotations into the recursive BuildSchema (Task G fix),
            // so annotations are applied exactly once at the outer level.
            var schema = new TypeSchema(new NullableTypeRef(new PrimitiveTypeRef(PrimitiveKind.Double)),
                                        new TypeAnnotations { Unit = "V", Minimum = 0 },
                                        ImmutableDictionary<string, TypeAnnotations>.Empty);
            var actual = schema.ToJsonSchema().ToJsonString();

            var expected = "{\"type\":[\"number\",\"null\"],\"format\":\"double\"," + "\"minimum\":0,\"x-unit\":\"V\"}";
            Assert.AreEqual(expected, actual);
        }

        // ── FromJsonSchema round-trip tests (§2.14) ────────────────────────────────────────────────

        [TestMethod]
        [DataRow(PrimitiveKind.Bool)]
        [DataRow(PrimitiveKind.String)]
        [DataRow(PrimitiveKind.Byte)]
        [DataRow(PrimitiveKind.Short)]
        [DataRow(PrimitiveKind.UShort)]
        [DataRow(PrimitiveKind.Int)]
        [DataRow(PrimitiveKind.UInt)]
        [DataRow(PrimitiveKind.Long)]
        [DataRow(PrimitiveKind.Float)]
        [DataRow(PrimitiveKind.Double)]
        [DataRow(PrimitiveKind.DateTime)]
        [DataRow(PrimitiveKind.Duration)]
        public void RoundtripPrimitiveTypeRefThroughJsonSchema(PrimitiveKind kind)
        {
            var original = TypeSchema.Of(new PrimitiveTypeRef(kind));
            var parsed = TypeSchemaSerialization.FromJsonSchema(original.ToJsonSchema());
            Assert.AreEqual(original.Type, parsed.Type);
        }

        [TestMethod]
        public void RoundtripPrimitiveWithAnnotationsPreservesAllFields()
        {
            var original = new TypeSchema(new PrimitiveTypeRef(PrimitiveKind.Double),
                                          new TypeAnnotations { Title = "Voltage", Description = "Setpoint", Unit = "V", Minimum = 0, Maximum = 250, ReadOnly = true },
                                          ImmutableDictionary<string, TypeAnnotations>.Empty);
            var parsed = TypeSchemaSerialization.FromJsonSchema(original.ToJsonSchema());
            Assert.AreEqual(original, parsed);
        }

        [TestMethod]
        public void RoundtripEnumPreservesIdentity()
        {
            var original = TypeSchema.Of(new EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical")));
            var parsed = TypeSchemaSerialization.FromJsonSchema(original.ToJsonSchema());
            Assert.AreEqual(original, parsed); // full TypeSchema equality; tests title not duplicated into annotations
        }

        [TestMethod]
        public void RoundtripStructPreservesIdentity()
        {
            var struc = new StructTypeRef("Coordinates",
                                          ImmutableArray.Create(new StructField("lat", new PrimitiveTypeRef(PrimitiveKind.Double)),
                                                                new StructField("lon", new PrimitiveTypeRef(PrimitiveKind.Double))),
                                          ImmutableArray.Create("lat", "lon"));
            var original = TypeSchema.Of(struc);
            var parsed = TypeSchemaSerialization.FromJsonSchema(original.ToJsonSchema());
            Assert.AreEqual(original, parsed);
        }

        [TestMethod]
        public void RoundtripStructWithPerFieldAnnotationsPreservesAllAnnotations()
        {
            var s = new StructTypeRef("Coordinates3D",
                                      ImmutableArray.Create(new StructField("lat", new PrimitiveTypeRef(PrimitiveKind.Double)),
                                                            new StructField("lon", new PrimitiveTypeRef(PrimitiveKind.Double)),
                                                            new StructField("altitude", new PrimitiveTypeRef(PrimitiveKind.Double))),
                                      ImmutableArray.Create("lat", "lon", "altitude"));
            var sfa = ImmutableDictionary<string, TypeAnnotations>.Empty
                                                                  .Add("lat", new TypeAnnotations { Unit = "deg", Minimum = -90, Maximum = 90 })
                                                                  .Add("lon", new TypeAnnotations { Unit = "deg", Minimum = -180, Maximum = 180 })
                                                                  .Add("altitude", new TypeAnnotations { Unit = "m" });
            var original = new TypeSchema(s, TypeAnnotations.None, sfa);
            var parsed = TypeSchemaSerialization.FromJsonSchema(original.ToJsonSchema());
            Assert.AreEqual(original, parsed);
        }

        [TestMethod]
        public void RoundtripNullablePrimitive()
        {
            var original = TypeSchema.Of(new NullableTypeRef(new PrimitiveTypeRef(PrimitiveKind.Double)));
            var parsed = TypeSchemaSerialization.FromJsonSchema(original.ToJsonSchema());
            Assert.AreEqual(original.Type, parsed.Type);
        }

        [TestMethod]
        public void RoundtripNullableEnum()
        {
            var original = TypeSchema.Of(new NullableTypeRef(new EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning"))));
            var parsed = TypeSchemaSerialization.FromJsonSchema(original.ToJsonSchema());
            Assert.AreEqual(original.Type, parsed.Type);
        }

        [TestMethod]
        public void RoundtripArrayOfNullableStruct()
        {
            var struc = new StructTypeRef("Coordinates",
                                          ImmutableArray.Create(new StructField("lat", new PrimitiveTypeRef(PrimitiveKind.Double)),
                                                                new StructField("lon", new PrimitiveTypeRef(PrimitiveKind.Double))),
                                          ImmutableArray.Create("lat", "lon"));
            var original = TypeSchema.Of(new ArrayTypeRef(new NullableTypeRef(struc)));
            var parsed = TypeSchemaSerialization.FromJsonSchema(original.ToJsonSchema());
            Assert.AreEqual(original.Type, parsed.Type);
        }

        // ── Profile rejection tests ────────────────────────────────────────────────────────────────

        [TestMethod]
        [DataRow("{\"$ref\":\"#/defs/X\"}")]
        [DataRow("{\"oneOf\":[{\"type\":\"string\"},{\"type\":\"number\"}]}")]
        [DataRow("{\"type\":\"string\",\"pattern\":\"[a-z]+\"}")]
        [DataRow("{\"type\":\"string\",\"minLength\":5}")]
        [DataRow("{\"type\":\"number\",\"exclusiveMinimum\":0}")]
        [DataRow("{\"type\":\"number\",\"multipleOf\":2}")]
        [DataRow("{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"minItems\":1}")]
        [DataRow("{\"type\":\"array\",\"items\":{\"type\":\"array\",\"items\":{\"type\":\"number\"}}}")]
        [DataRow("{\"type\":\"object\",\"title\":\"X\",\"properties\":{},\"required\":[],\"patternProperties\":{}}")]
        [DataRow("{\"type\":\"object\",\"title\":\"X\",\"properties\":{},\"required\":[],\"additionalProperties\":true}")]
        public void RejectNonProfileKeywordsWithInvalidSchemaException(string schemaJson)
        {
            var node = JsonNode.Parse(schemaJson)!;
            Assert.Throws<InvalidSchemaException>(() => TypeSchemaSerialization.FromJsonSchema(node));
        }

        [TestMethod]
        public void RejectMissingTypeKeyword()
        {
            var node = JsonNode.Parse("{\"title\":\"X\"}")!;
            Assert.Throws<InvalidSchemaException>(() => TypeSchemaSerialization.FromJsonSchema(node));
        }

        [TestMethod]
        public void RejectEnumWithoutTitle()
        {
            var node = JsonNode.Parse("{\"type\":\"string\",\"enum\":[\"A\",\"B\"]}")!;
            Assert.Throws<InvalidSchemaException>(() => TypeSchemaSerialization.FromJsonSchema(node));
        }

        [TestMethod]
        public void RejectStructWithoutTitle()
        {
            var node = JsonNode.Parse("{\"type\":\"object\",\"properties\":{},\"required\":[],\"additionalProperties\":false}")!;
            Assert.Throws<InvalidSchemaException>(() => TypeSchemaSerialization.FromJsonSchema(node));
        }
    }
}