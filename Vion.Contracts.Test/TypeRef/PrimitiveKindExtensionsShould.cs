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
        public void ReturnGuidForGuidKind()
        {
            // Act
            var type = PrimitiveKind.Guid.ClrType();

            // Assert
            Assert.AreEqual(typeof(Guid), type);
        }

        [TestMethod]
        public void ReturnNullableBoolForBoolKindWhenIsNullable()
        {
            // Act
            var type = PrimitiveKind.Bool.ClrType(true);

            // Assert
            Assert.AreEqual(typeof(bool?), type);
        }

        [TestMethod]
        public void ReturnNullableLongForIntKindWhenIsNullable()
        {
            // Act
            var type = PrimitiveKind.Int.ClrType(true);

            // Assert
            Assert.AreEqual(typeof(long?), type);
        }

        [TestMethod]
        public void ReturnNullableDoubleForFloatKindWhenIsNullable()
        {
            // Act
            var type = PrimitiveKind.Float.ClrType(true);

            // Assert
            Assert.AreEqual(typeof(double?), type);
        }

        [TestMethod]
        public void ReturnStringForStringKindWhenIsNullable()
        {
            // Act
            var type = PrimitiveKind.String.ClrType(true);

            // Assert
            Assert.AreEqual(typeof(string), type);
        }

        [TestMethod]
        public void ReturnNullableDateTimeForDateTimeKindWhenIsNullable()
        {
            // Act
            var type = PrimitiveKind.DateTime.ClrType(true);

            // Assert
            Assert.AreEqual(typeof(DateTime?), type);
        }

        [TestMethod]
        public void ReturnNullableTimeSpanForDurationKindWhenIsNullable()
        {
            // Act
            var type = PrimitiveKind.Duration.ClrType(true);

            // Assert
            Assert.AreEqual(typeof(TimeSpan?), type);
        }

        [TestMethod]
        public void ReturnNullableGuidForGuidKindWhenIsNullable()
        {
            // Act
            var type = PrimitiveKind.Guid.ClrType(true);

            // Assert
            Assert.AreEqual(typeof(Guid?), type);
        }

        [TestMethod]
        public void HandleEveryPrimitiveKind()
        {
            // Guard: every kind must resolve to a CLR type without falling through to the
            // NotSupportedException default. Adding a PrimitiveKind without extending ClrType fails here.
            foreach (var kind in Enum.GetValues<PrimitiveKind>())
            {
                Assert.IsNotNull(kind.ClrType(), $"ClrType() did not handle {kind}.");
                Assert.IsNotNull(kind.ClrType(true), $"ClrType(true) did not handle {kind}.");
            }
        }
    }
}
