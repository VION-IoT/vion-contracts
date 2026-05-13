using System;
using Vion.Contracts.TypeRef;

namespace Vion.Contracts.Test.TypeRef
{
    [TestClass]
    public class PrimitiveKindExtensionsShould
    {
        [TestMethod]
        public void ReturnBoolForBoolKind()
        {
            // Act
            var type = PrimitiveKind.Bool.ClrType();

            // Assert
            Assert.AreEqual(typeof(bool), type);
        }

        [TestMethod]
        public void ReturnLongForByteKind()
        {
            // Act
            var type = PrimitiveKind.Byte.ClrType();

            // Assert
            Assert.AreEqual(typeof(long), type);
        }

        [TestMethod]
        public void ReturnLongForShortKind()
        {
            // Act
            var type = PrimitiveKind.Short.ClrType();

            // Assert
            Assert.AreEqual(typeof(long), type);
        }

        [TestMethod]
        public void ReturnLongForUShortKind()
        {
            // Act
            var type = PrimitiveKind.UShort.ClrType();

            // Assert
            Assert.AreEqual(typeof(long), type);
        }

        [TestMethod]
        public void ReturnLongForIntKind()
        {
            // Act
            var type = PrimitiveKind.Int.ClrType();

            // Assert
            Assert.AreEqual(typeof(long), type);
        }

        [TestMethod]
        public void ReturnLongForUIntKind()
        {
            // Act
            var type = PrimitiveKind.UInt.ClrType();

            // Assert
            Assert.AreEqual(typeof(long), type);
        }

        [TestMethod]
        public void ReturnLongForLongKind()
        {
            // Act
            var type = PrimitiveKind.Long.ClrType();

            // Assert
            Assert.AreEqual(typeof(long), type);
        }

        [TestMethod]
        public void ReturnDoubleForFloatKind()
        {
            // Act
            var type = PrimitiveKind.Float.ClrType();

            // Assert
            Assert.AreEqual(typeof(double), type);
        }

        [TestMethod]
        public void ReturnDoubleForDoubleKind()
        {
            // Act
            var type = PrimitiveKind.Double.ClrType();

            // Assert
            Assert.AreEqual(typeof(double), type);
        }

        [TestMethod]
        public void ReturnStringForStringKind()
        {
            // Act
            var type = PrimitiveKind.String.ClrType();

            // Assert
            Assert.AreEqual(typeof(string), type);
        }

        [TestMethod]
        public void ReturnDateTimeForDateTimeKind()
        {
            // Act
            var type = PrimitiveKind.DateTime.ClrType();

            // Assert
            Assert.AreEqual(typeof(DateTime), type);
        }

        [TestMethod]
        public void ReturnTimeSpanForDurationKind()
        {
            // Act
            var type = PrimitiveKind.Duration.ClrType();

            // Assert
            Assert.AreEqual(typeof(TimeSpan), type);
        }

        [TestMethod]
        public void ReturnNullableBoolForBoolKindWhenIsNullable()
        {
            // Act
            var type = PrimitiveKind.Bool.ClrType(isNullable: true);

            // Assert
            Assert.AreEqual(typeof(bool?), type);
        }

        [TestMethod]
        public void ReturnNullableLongForIntKindWhenIsNullable()
        {
            // Act
            var type = PrimitiveKind.Int.ClrType(isNullable: true);

            // Assert
            Assert.AreEqual(typeof(long?), type);
        }

        [TestMethod]
        public void ReturnNullableDoubleForFloatKindWhenIsNullable()
        {
            // Act
            var type = PrimitiveKind.Float.ClrType(isNullable: true);

            // Assert
            Assert.AreEqual(typeof(double?), type);
        }

        [TestMethod]
        public void ReturnStringForStringKindWhenIsNullable()
        {
            // Act
            var type = PrimitiveKind.String.ClrType(isNullable: true);

            // Assert
            Assert.AreEqual(typeof(string), type);
        }

        [TestMethod]
        public void ReturnNullableDateTimeForDateTimeKindWhenIsNullable()
        {
            // Act
            var type = PrimitiveKind.DateTime.ClrType(isNullable: true);

            // Assert
            Assert.AreEqual(typeof(DateTime?), type);
        }

        [TestMethod]
        public void ReturnNullableTimeSpanForDurationKindWhenIsNullable()
        {
            // Act
            var type = PrimitiveKind.Duration.ClrType(isNullable: true);

            // Assert
            Assert.AreEqual(typeof(TimeSpan?), type);
        }
    }
}
