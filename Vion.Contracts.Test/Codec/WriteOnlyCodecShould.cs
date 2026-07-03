using System;
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Vion.Contracts.Codec;
using Vion.Contracts.Conventions;
using Vion.Contracts.TypeRef;
using TR = Vion.Contracts.TypeRef;

namespace Vion.Contracts.Test.Codec
{
    [TestClass]
    public class WriteOnlyCodecShould
    {
        private static readonly TypeSchema PlainSchema = TypeSchema.Of(new PrimitiveTypeRef(PrimitiveKind.String));

        private static readonly TypeSchema SecretSchema = new(new NullableTypeRef(new PrimitiveTypeRef(PrimitiveKind.String)),
                                                              new TypeAnnotations { WriteOnly = true },
                                                              ImmutableDictionary<string, TypeAnnotations>.Empty);

        private static readonly TypeSchema StructSchema = BuildStructSchema(new StructTypeRef("Struct", StructFields, []));

        private static readonly TypeSchema StructArraySchema = BuildStructSchema(new ArrayTypeRef(new StructTypeRef("Struct", StructFields, [])));

        private static ImmutableArray<StructField> StructFields
        {
            get =>
            [
                new("Plain", new PrimitiveTypeRef(PrimitiveKind.String)),
                new("Secret", new NullableTypeRef(new PrimitiveTypeRef(PrimitiveKind.String))),
            ];
        }

        [TestMethod]
        public void RedactSetSecret()
        {
            // Arrange

            // Act
            var redacted = WriteOnlyCodec.Redact(SecretSchema, JsonValue.Create(Guid.NewGuid().ToString()));

            // Assert
            Assert.AreEqual(WriteOnlyConventions.RedactedSentinel, redacted!.GetValue<string>());
        }

        [TestMethod]
        public void KeepUnsetSecretNullWhenRedacting()
        {
            // Arrange

            // Act
            var redacted = WriteOnlyCodec.Redact(SecretSchema, null);

            // Assert
            Assert.IsNull(redacted);
        }

        [TestMethod]
        public void LeaveNonSecretValueUnchangedWhenRedacting()
        {
            // Arrange
            var value = JsonValue.Create(Guid.NewGuid().ToString());

            // Act
            var redacted = WriteOnlyCodec.Redact(PlainSchema, value);

            // Assert
            Assert.AreSame(value, redacted);
        }

        [TestMethod]
        public void LeaveNullStructUnchangedWhenRedacting()
        {
            // Arrange

            // Act
            var redacted = WriteOnlyCodec.Redact(StructSchema, null);

            // Assert
            Assert.IsNull(redacted);
        }

        [TestMethod]
        public void RedactOnlySecretMember()
        {
            // Arrange
            var plain = Guid.NewGuid().ToString();
            var value = new JsonObject { ["Plain"] = plain, ["Secret"] = Guid.NewGuid().ToString() };

            // Act
            var redacted = WriteOnlyCodec.Redact(StructSchema, value);

            // Assert
            var expectedValue = new JsonObject { ["Plain"] = plain, ["Secret"] = WriteOnlyConventions.RedactedSentinel };
            Assert.IsTrue(JsonNode.DeepEquals(expectedValue, redacted));
        }

        [TestMethod]
        public void KeepUnsetSecretMemberNull()
        {
            // Arrange
            var value = new JsonObject { ["Plain"] = Guid.NewGuid().ToString(), ["Secret"] = null };

            // Act
            var redacted = WriteOnlyCodec.Redact(StructSchema, value);

            // Assert
            Assert.IsTrue(JsonNode.DeepEquals(value, redacted));
        }

        [TestMethod]
        public void RedactSecretMemberInNullableStruct()
        {
            // Arrange
            var schema = BuildStructSchema(new NullableTypeRef(new StructTypeRef("Struct", StructFields, [])));
            var value = new JsonObject { ["Secret"] = Guid.NewGuid().ToString() };

            // Act
            var redacted = WriteOnlyCodec.Redact(schema, value);

            // Assert
            Assert.AreEqual(WriteOnlyConventions.RedactedSentinel, redacted!["Secret"]!.GetValue<string>());
        }

        [TestMethod]
        public void NotMutateCallerValueWhenRedacting()
        {
            // Arrange
            var secret = Guid.NewGuid().ToString();
            var value = new JsonObject { ["Secret"] = secret };

            // Act
            WriteOnlyCodec.Redact(StructSchema, value);

            // Assert
            Assert.AreEqual(secret, value["Secret"]!.GetValue<string>());
        }

        [TestMethod]
        public void RedactSecretMemberInEveryArrayItem()
        {
            // Arrange
            var value = new JsonArray(new JsonObject { ["Secret"] = Guid.NewGuid().ToString() }, new JsonObject { ["Secret"] = null });

            // Act
            var redacted = WriteOnlyCodec.Redact(StructArraySchema, value);

            // Assert
            var expectedValue = new JsonArray(new JsonObject { ["Secret"] = WriteOnlyConventions.RedactedSentinel }, new JsonObject { ["Secret"] = null });
            Assert.IsTrue(JsonNode.DeepEquals(expectedValue, redacted));
        }

        [TestMethod]
        public void NotMutateCallerArrayWhenRedacting()
        {
            // Arrange
            var secret = Guid.NewGuid().ToString();
            var value = new JsonArray(new JsonObject { ["Secret"] = secret });

            // Act
            WriteOnlyCodec.Redact(StructArraySchema, value);

            // Assert
            Assert.AreEqual(secret, value[0]!["Secret"]!.GetValue<string>());
        }

        [TestMethod]
        public void KeepStoredSecretWhenRedactedValueSentBack()
        {
            // Arrange
            var stored = JsonValue.Create(Guid.NewGuid().ToString());

            // Act
            var resolved = WriteOnlyCodec.Resolve(SecretSchema, JsonValue.Create(WriteOnlyConventions.RedactedSentinel), stored);

            // Assert
            Assert.AreEqual(stored.GetValue<string>(), resolved!.GetValue<string>());
        }

        [TestMethod]
        public void ReplaceStoredSecretWhenNewValueSent()
        {
            // Arrange
            var sent = JsonValue.Create(Guid.NewGuid().ToString());

            // Act
            var resolved = WriteOnlyCodec.Resolve(SecretSchema, sent, JsonValue.Create(Guid.NewGuid().ToString()));

            // Assert
            Assert.AreSame(sent, resolved);
        }

        [TestMethod]
        public void ClearSecretWhenNullSent()
        {
            // Arrange

            // Act
            var resolved = WriteOnlyCodec.Resolve(SecretSchema, null, JsonValue.Create(Guid.NewGuid().ToString()));

            // Assert
            Assert.IsNull(resolved);
        }

        [TestMethod]
        public void LeaveNonSecretValueUnchangedWhenResolving()
        {
            // Arrange
            var sent = JsonValue.Create(Guid.NewGuid().ToString());

            // Act
            var resolved = WriteOnlyCodec.Resolve(PlainSchema, sent, JsonValue.Create(Guid.NewGuid().ToString()));

            // Assert
            Assert.AreSame(sent, resolved);
        }

        [TestMethod]
        public void ClearStructWhenNullSent()
        {
            // Arrange
            var stored = new JsonObject { ["Secret"] = Guid.NewGuid().ToString() };

            // Act
            var resolved = WriteOnlyCodec.Resolve(StructSchema, null, stored);

            // Assert
            Assert.IsNull(resolved);
        }

        [TestMethod]
        public void KeepStoredSecretMemberWhenRedactedValueSentBack()
        {
            // Arrange
            var plain = Guid.NewGuid().ToString();
            var secret = Guid.NewGuid().ToString();
            var sent = new JsonObject { ["Plain"] = plain, ["Secret"] = WriteOnlyConventions.RedactedSentinel };
            var stored = new JsonObject { ["Plain"] = Guid.NewGuid().ToString(), ["Secret"] = secret };

            // Act
            var resolved = WriteOnlyCodec.Resolve(StructSchema, sent, stored);

            // Assert
            var expectedValue = new JsonObject { ["Plain"] = plain, ["Secret"] = secret };
            Assert.IsTrue(JsonNode.DeepEquals(expectedValue, resolved));
        }

        [TestMethod]
        public void ClearSecretMemberWhenNullSent()
        {
            // Arrange
            var sent = new JsonObject { ["Secret"] = null };
            var stored = new JsonObject { ["Secret"] = Guid.NewGuid().ToString() };

            // Act
            var resolved = WriteOnlyCodec.Resolve(StructSchema, sent, stored);

            // Assert
            Assert.IsTrue(JsonNode.DeepEquals(sent, resolved));
        }

        [TestMethod]
        public void ClearSecretMemberWhenNothingStored()
        {
            // Arrange
            var sent = new JsonObject { ["Secret"] = WriteOnlyConventions.RedactedSentinel };

            // Act
            var resolved = WriteOnlyCodec.Resolve(StructSchema, sent, null);

            // Assert
            Assert.IsNull(resolved!["Secret"]);
        }

        [TestMethod]
        public void KeepNonStringValueAtSecretPosition()
        {
            // Arrange
            var sent = new JsonObject { ["Secret"] = 42 };
            var stored = new JsonObject { ["Secret"] = Guid.NewGuid().ToString() };

            // Act
            var resolved = WriteOnlyCodec.Resolve(StructSchema, sent, stored);

            // Assert
            Assert.AreEqual(42, resolved!["Secret"]!.GetValue<int>());
        }

        [TestMethod]
        public void NotMutateSentValueWhenResolving()
        {
            // Arrange
            var sent = new JsonObject { ["Secret"] = WriteOnlyConventions.RedactedSentinel };
            var stored = new JsonObject { ["Secret"] = Guid.NewGuid().ToString() };

            // Act
            WriteOnlyCodec.Resolve(StructSchema, sent, stored);

            // Assert
            Assert.AreEqual(WriteOnlyConventions.RedactedSentinel, sent["Secret"]!.GetValue<string>());
        }

        [TestMethod]
        public void NotMutateStoredValueWhenResolving()
        {
            // Arrange
            var sent = new JsonObject { ["Secret"] = WriteOnlyConventions.RedactedSentinel };
            var expectedStored = new JsonObject { ["Secret"] = Guid.NewGuid().ToString() };
            var stored = expectedStored.DeepClone();

            // Act
            WriteOnlyCodec.Resolve(StructSchema, sent, stored);

            // Assert
            Assert.IsTrue(JsonNode.DeepEquals(expectedStored, stored));
        }

        [TestMethod]
        public void KeepStoredSecretForArrayItemAtSamePosition()
        {
            // Arrange
            var secret = Guid.NewGuid().ToString();
            var sent = new JsonArray(new JsonObject { ["Secret"] = WriteOnlyConventions.RedactedSentinel });
            var stored = new JsonArray(new JsonObject { ["Secret"] = secret }, new JsonObject { ["Secret"] = Guid.NewGuid().ToString() });

            // Act
            var resolved = WriteOnlyCodec.Resolve(StructArraySchema, sent, stored);

            // Assert
            var expectedValue = new JsonArray(new JsonObject { ["Secret"] = secret });
            Assert.IsTrue(JsonNode.DeepEquals(expectedValue, resolved));
        }

        [TestMethod]
        public void ClearArrayItemSecretWhenNoStoredItemAtSamePosition()
        {
            // Arrange
            var secret = Guid.NewGuid().ToString();
            var sent = new JsonArray(new JsonObject { ["Secret"] = WriteOnlyConventions.RedactedSentinel }, new JsonObject { ["Secret"] = WriteOnlyConventions.RedactedSentinel });
            var stored = new JsonArray(new JsonObject { ["Secret"] = secret });

            // Act
            var resolved = WriteOnlyCodec.Resolve(StructArraySchema, sent, stored);

            // Assert
            Assert.AreEqual(secret, resolved![0]!["Secret"]!.GetValue<string>());
            Assert.IsNull(resolved[1]!["Secret"]);
        }

        private static TypeSchema BuildStructSchema(TR.TypeRef type)
        {
            return new TypeSchema(type, TypeAnnotations.None, ImmutableDictionary<string, TypeAnnotations>.Empty.Add("Secret", new TypeAnnotations { WriteOnly = true }));
        }
    }
}
