using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Xml;
using Vion.Contracts.Codec;
using Vion.Contracts.TypeRef;

namespace Vion.Contracts.Test.Codec
{
    [TestClass]
    public class JsonNodeExtensionsShould
    {
        [TestMethod]
        public void ConvertJsonNodeValueToBool()
        {
            // Arrange
            const bool expected = true;
            JsonNode value = JsonValue.Create(expected);

            // Act
            var result = value.ToClrPrimitive(PrimitiveKind.Bool);

            // Assert
            Assert.IsInstanceOfType<bool>(result);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ConvertJsonNodeValueToLongForByteKind()
        {
            // Arrange
            const long expected = 200L;
            JsonNode value = JsonValue.Create(expected);

            // Act
            var result = value.ToClrPrimitive(PrimitiveKind.Byte);

            // Assert
            Assert.IsInstanceOfType<long>(result);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ConvertJsonNodeValueToLongForShortKind()
        {
            // Arrange
            const long expected = -1000L;
            JsonNode value = JsonValue.Create(expected);

            // Act
            var result = value.ToClrPrimitive(PrimitiveKind.Short);

            // Assert
            Assert.IsInstanceOfType<long>(result);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ConvertJsonNodeValueToLongForUShortKind()
        {
            // Arrange
            const long expected = 65000L;
            JsonNode value = JsonValue.Create(expected);

            // Act
            var result = value.ToClrPrimitive(PrimitiveKind.UShort);

            // Assert
            Assert.IsInstanceOfType<long>(result);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ConvertJsonNodeValueToLongForIntKind()
        {
            // Arrange
            const long expected = 42L;
            JsonNode value = JsonValue.Create(expected);

            // Act
            var result = value.ToClrPrimitive(PrimitiveKind.Int);

            // Assert
            Assert.IsInstanceOfType<long>(result);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ConvertJsonNodeValueToLongForUIntKind()
        {
            // Arrange
            const long expected = 4000000000L;
            JsonNode value = JsonValue.Create(expected);

            // Act
            var result = value.ToClrPrimitive(PrimitiveKind.UInt);

            // Assert
            Assert.IsInstanceOfType<long>(result);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ConvertJsonNodeValueToLongForLongKind()
        {
            // Arrange
            const long expected = 10_000_000_000L;
            JsonNode value = JsonValue.Create(expected);

            // Act
            var result = value.ToClrPrimitive(PrimitiveKind.Long);

            // Assert
            Assert.IsInstanceOfType<long>(result);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ConvertJsonNodeValueToDoubleForFloatKind()
        {
            // Arrange
            const double expected = 3.14;
            JsonNode value = JsonValue.Create(expected);

            // Act
            var result = value.ToClrPrimitive(PrimitiveKind.Float);

            // Assert
            Assert.IsInstanceOfType<double>(result);
            Assert.AreEqual(expected, (double)result, 1e-9);
        }

        [TestMethod]
        public void ConvertJsonNodeValueToDoubleForDoubleKind()
        {
            // Arrange
            const double expected = 3.14;
            JsonNode value = JsonValue.Create(expected);

            // Act
            var result = value.ToClrPrimitive(PrimitiveKind.Double);

            // Assert
            Assert.IsInstanceOfType<double>(result);
            Assert.AreEqual(expected, (double)result, 1e-9);
        }

        [TestMethod]
        public void ConvertJsonNodeValueToString()
        {
            // Arrange
            const string expected = "hello";
            JsonNode value = JsonValue.Create(expected);

            // Act
            var result = value.ToClrPrimitive(PrimitiveKind.String);

            // Assert
            Assert.IsInstanceOfType<string>(result);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ConvertJsonNodeValueToDateTime()
        {
            // Arrange
            var expected = new DateTime(2026,
                                        5,
                                        12,
                                        10,
                                        30,
                                        0,
                                        DateTimeKind.Utc);
            JsonNode value = JsonValue.Create(expected.ToString("O"));

            // Act
            var result = value.ToClrPrimitive(PrimitiveKind.DateTime);

            // Assert
            Assert.IsInstanceOfType<DateTime>(result);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ConvertJsonNodeValueToDuration()
        {
            // Arrange
            var expected = new TimeSpan(1, 30, 0);
            JsonNode value = JsonValue.Create(XmlConvert.ToString(expected));

            // Act
            var result = value.ToClrPrimitive(PrimitiveKind.Duration);

            // Assert
            Assert.IsInstanceOfType<TimeSpan>(result);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ConvertJsonNodeValueToGuid()
        {
            // Arrange
            var expected = new Guid("12345678-1234-1234-1234-1234567890ab");
            JsonNode value = JsonValue.Create(expected.ToString("D"));

            // Act
            var result = value.ToClrPrimitive(PrimitiveKind.Guid);

            // Assert
            Assert.IsInstanceOfType<Guid>(result);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ReturnNullForNullJsonNodeValue()
        {
            // Arrange
            JsonNode? value = null;

            // Act
            var result = value.ToClrPrimitive(PrimitiveKind.Bool);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void HandleEveryPrimitiveKind()
        {
            // Arrange - A representative JSON value per kind. This map must cover every PrimitiveKind: the
            // coverage assertion below trips when a new kind is added without a sample here, forcing
            // ToClrPrimitive to be revisited at the same time.
            var samples = new Dictionary<PrimitiveKind, JsonNode?>
                          {
                              [PrimitiveKind.Bool] = JsonValue.Create(true),
                              [PrimitiveKind.String] = JsonValue.Create("x"),
                              [PrimitiveKind.Byte] = JsonValue.Create(1L),
                              [PrimitiveKind.Short] = JsonValue.Create(1L),
                              [PrimitiveKind.UShort] = JsonValue.Create(1L),
                              [PrimitiveKind.Int] = JsonValue.Create(1L),
                              [PrimitiveKind.UInt] = JsonValue.Create(1L),
                              [PrimitiveKind.Long] = JsonValue.Create(1L),
                              [PrimitiveKind.Float] = JsonValue.Create(1.0),
                              [PrimitiveKind.Double] = JsonValue.Create(1.0),
                              [PrimitiveKind.DateTime] = JsonValue.Create("2026-01-01T00:00:00.0000000Z"),
                              [PrimitiveKind.Duration] = JsonValue.Create("PT1H"),
                              [PrimitiveKind.Guid] = JsonValue.Create("12345678-1234-1234-1234-1234567890ab"),
                          };

            // Guard: the sample map covers every kind. A new PrimitiveKind trips this until a sample is added.
            CollectionAssert.AreEquivalent(Enum.GetValues<PrimitiveKind>(), samples.Keys.ToArray());

            // Every kind converts without falling through to the NotSupportedException default.
            foreach (var (kind, node) in samples)
            {
                // Act
                var result = node.ToClrPrimitive(kind);

                // Assert
                Assert.IsNotNull(result, $"ToClrPrimitive did not handle {kind}.");
            }
        }
    }
}
