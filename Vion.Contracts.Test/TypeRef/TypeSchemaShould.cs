using System;
using System.Collections.Immutable;
using Vion.Contracts.TypeRef;

namespace Vion.Contracts.Test.TypeRef
{
    [TestClass]
    public class TypeSchemaShould
    {
        [TestMethod]
        public void HaveEqualTypeRefButUnequalSchemaWhenAnnotationsDiffer()
        {
            var t = new PrimitiveTypeRef(PrimitiveKind.Double);
            var s1 = new TypeSchema(t, new TypeAnnotations { Unit = "V" }, ImmutableDictionary<string, TypeAnnotations>.Empty);
            var s2 = new TypeSchema(t, new TypeAnnotations { Unit = "A" }, ImmutableDictionary<string, TypeAnnotations>.Empty);

            Assert.AreNotEqual(s1, s2); // different annotations -> different schemas
            Assert.AreEqual(s1.Type, s2.Type); // same TypeRef identity
            Assert.AreEqual(s1.Type.GetHashCode(), s2.Type.GetHashCode());
        }

        [TestMethod]
        public void RecognizeTwoSchemasWithSameContentAsEqual()
        {
            var t = new PrimitiveTypeRef(PrimitiveKind.Double);
            var ann = new TypeAnnotations { Unit = "V", Minimum = 0, Maximum = 250 };
            var s1 = new TypeSchema(t, ann, ImmutableDictionary<string, TypeAnnotations>.Empty);
            var s2 = new TypeSchema(t, ann, ImmutableDictionary<string, TypeAnnotations>.Empty);

            Assert.AreEqual(s1, s2);
            Assert.AreEqual(s1.GetHashCode(), s2.GetHashCode());
        }

        [TestMethod]
        public void RejectNullStructFieldAnnotationsAtConstruction()
        {
            // StructFieldAnnotations must be non-null; null is rejected at construction so that
            // invalid instances cannot exist. Mirrors the IsDefault guards on
            // EnumTypeRef.Members and StructTypeRef.Fields/Required.
            var t = new PrimitiveTypeRef(PrimitiveKind.Double);
            Assert.Throws<ArgumentNullException>(() => new TypeSchema(t, TypeAnnotations.None, null!));
        }

        [TestMethod]
        public void TreatStructFieldAnnotationsWithDifferentInsertionOrderAsEqual()
        {
            // Equality uses OrderBy(kv => kv.Key), so dictionaries built with the same key/value
            // pairs but inserted in different order must compare equal and have equal hash codes.
            var t = new PrimitiveTypeRef(PrimitiveKind.Double);
            var unitV = new TypeAnnotations { Unit = "V" };
            var unitA = new TypeAnnotations { Unit = "A" };

            var s1 = new TypeSchema(t, TypeAnnotations.None, ImmutableDictionary<string, TypeAnnotations>.Empty.Add("voltage", unitV).Add("current", unitA));
            var s2 = new TypeSchema(t, TypeAnnotations.None, ImmutableDictionary<string, TypeAnnotations>.Empty.Add("current", unitA).Add("voltage", unitV));

            Assert.AreEqual(s1, s2);
            Assert.AreEqual(s1.GetHashCode(), s2.GetHashCode());
        }
    }
}
