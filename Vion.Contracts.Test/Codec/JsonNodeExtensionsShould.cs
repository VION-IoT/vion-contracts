using System;
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
        public void ReturnNullForNullJsonNodeValue()
        {
            // Arrange
            JsonNode? value = null;

            // Act
            var result = value.ToClrPrimitive(PrimitiveKind.Bool);

            // Assert
            Assert.IsNull(result);
        }
    }
}
