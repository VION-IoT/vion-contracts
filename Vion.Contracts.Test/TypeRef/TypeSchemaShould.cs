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
        public void TreatNullStructFieldAnnotationsAsDistinct()
        {
            var t = new PrimitiveTypeRef(PrimitiveKind.Double);
            var populated = new TypeSchema(t, TypeAnnotations.None, ImmutableDictionary<string, TypeAnnotations>.Empty);
            var nullified = new TypeSchema(t, TypeAnnotations.None, null!);

            // Null StructFieldAnnotations is treated as "unknown" — not equal to any other instance,
            // including another null. Mirrors the IsDefault guard pattern on EnumTypeRef/StructTypeRef.
            Assert.AreNotEqual(populated, nullified);

            var alsoNull = new TypeSchema(t, TypeAnnotations.None, null!);
            Assert.AreNotEqual(nullified, alsoNull);
        }
    }
}
