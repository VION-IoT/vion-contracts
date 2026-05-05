using System;
using System.Collections.Immutable;
using Vion.Contracts.TypeRef;

namespace Vion.Contracts.Test.TypeRef
{
    [TestClass]
    public class TypeRefIdentityShould
    {
        [TestMethod]
        public void RecognizeSamePrimitiveKindsAsEqual()
        {
            var a = new PrimitiveTypeRef(PrimitiveKind.Double);
            var b = new PrimitiveTypeRef(PrimitiveKind.Double);
            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void DistinguishDifferentPrimitiveKinds()
        {
            var a = new PrimitiveTypeRef(PrimitiveKind.Double);
            var b = new PrimitiveTypeRef(PrimitiveKind.Float);
            Assert.AreNotEqual(a, b);
        }

        [TestMethod]
        public void RecognizeSameByteKindsAsEqual()
        {
            var a = new PrimitiveTypeRef(PrimitiveKind.Byte);
            var b = new PrimitiveTypeRef(PrimitiveKind.Byte);
            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void RecognizeSameUShortKindsAsEqual()
        {
            var a = new PrimitiveTypeRef(PrimitiveKind.UShort);
            var b = new PrimitiveTypeRef(PrimitiveKind.UShort);
            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void DistinguishSignedAndUnsignedOfSameWidth()
        {
            var signedShort = new PrimitiveTypeRef(PrimitiveKind.Short);
            var unsignedShort = new PrimitiveTypeRef(PrimitiveKind.UShort);
            Assert.AreNotEqual(signedShort, unsignedShort);

            var signedInt = new PrimitiveTypeRef(PrimitiveKind.Int);
            var unsignedInt = new PrimitiveTypeRef(PrimitiveKind.UInt);
            Assert.AreNotEqual(signedInt, unsignedInt);
        }

        [TestMethod]
        public void RecognizeEnumsWithSameTitleAndMembersAsEqual()
        {
            var a = new EnumTypeRef("AlarmState", ["Ok", "Warning", "Critical"]);
            var b = new EnumTypeRef("AlarmState", ["Ok", "Warning", "Critical"]);
            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void DistinguishEnumsWithDifferentTitles()
        {
            var a = new EnumTypeRef("AlarmState", ["Ok"]);
            var b = new EnumTypeRef("DoorState", ["Ok"]);
            Assert.AreNotEqual(a, b);
        }

        [TestMethod]
        public void RecognizeStructsWithSameTitleAndShapeAsEqual()
        {
            var a = new StructTypeRef("Coordinates",
                                      [
                                          new StructField("Lat", new PrimitiveTypeRef(PrimitiveKind.Double)), new StructField("Lon", new PrimitiveTypeRef(PrimitiveKind.Double)),
                                      ],
                                      ["Lat", "Lon"]);
            var b = new StructTypeRef("Coordinates",
                                      [
                                          new StructField("Lat", new PrimitiveTypeRef(PrimitiveKind.Double)), new StructField("Lon", new PrimitiveTypeRef(PrimitiveKind.Double)),
                                      ],
                                      ["Lat", "Lon"]);
            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void DistinguishStructsWithSameShapeButDifferentTitles()
        {
            var fields = ImmutableArray.Create(new StructField("X", new PrimitiveTypeRef(PrimitiveKind.Double)), new StructField("Y", new PrimitiveTypeRef(PrimitiveKind.Double)));
            var required = ImmutableArray.Create("X", "Y");
            var a = new StructTypeRef("Coordinates", fields, required);
            var b = new StructTypeRef("Pressure", fields, required);
            Assert.AreNotEqual(a, b);
        }

        [TestMethod]
        public void RecognizeArraysWithSameItemsAsEqual()
        {
            var a = new ArrayTypeRef(new PrimitiveTypeRef(PrimitiveKind.Double));
            var b = new ArrayTypeRef(new PrimitiveTypeRef(PrimitiveKind.Double));
            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void RecognizeNullableOfStructAsEqualWhenInnerEqual()
        {
            var struc = new StructTypeRef("Coordinates", ImmutableArray.Create(new StructField("Lat", new PrimitiveTypeRef(PrimitiveKind.Double))), ImmutableArray.Create("Lat"));
            var a = new NullableTypeRef(struc);
            var b = new NullableTypeRef(struc);
            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void DistinguishEnumsWithSameMembersInDifferentOrder()
        {
            var a = new EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical"));
            var b = new EnumTypeRef("AlarmState", ImmutableArray.Create("Warning", "Ok", "Critical"));
            Assert.AreNotEqual(a, b);
        }

        [TestMethod]
        public void RejectDefaultMembersAtConstruction()
        {
            // EnumTypeRef.Members must be initialized; default(ImmutableArray<T>) is rejected so that
            // invalid instances cannot exist. Mirrors StructTypeRef.Fields/Required and
            // TypeSchema.StructFieldAnnotations.
            Assert.Throws<ArgumentException>(() => new EnumTypeRef("AlarmState", default));
        }

        [TestMethod]
        public void RejectDefaultFieldsOrRequiredAtConstruction()
        {
            var fields = ImmutableArray.Create(new StructField("X", new PrimitiveTypeRef(PrimitiveKind.Double)));
            var required = ImmutableArray.Create("X");

            Assert.Throws<ArgumentException>(() => new StructTypeRef("Coordinates", default, required));
            Assert.Throws<ArgumentException>(() => new StructTypeRef("Coordinates", fields, default));
        }

        [TestMethod]
        public void DistinguishConcreteTypeRefsAcrossKinds()
        {
            var primitive = new PrimitiveTypeRef(PrimitiveKind.Double);
            var enumRef = new EnumTypeRef("Double", ImmutableArray.Create("A"));
            Assert.AreNotEqual<Vion.Contracts.TypeRef.TypeRef>(primitive, enumRef);
        }
    }
}