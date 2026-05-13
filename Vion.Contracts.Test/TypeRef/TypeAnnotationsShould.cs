using Vion.Contracts.TypeRef;

namespace Vion.Contracts.Test.TypeRef
{
    [TestClass]
    public class TypeAnnotationsShould
    {
        [TestMethod]
        public void RoundTripsDescription()
        {
            var a = new TypeAnnotations { Description = "Power flowing right now" };
            Assert.AreEqual("Power flowing right now", a.Description);
        }

        [TestMethod]
        public void RoundTripsWriteOnly()
        {
            var a = new TypeAnnotations { WriteOnly = true };
            Assert.IsTrue(a.WriteOnly);
        }

        [TestMethod]
        public void WriteOnlyDefaultsToFalse()
        {
            var a = new TypeAnnotations();
            Assert.IsFalse(a.WriteOnly);
        }

        [TestMethod]
        public void NewFieldsParticipateInEquality()
        {
            Assert.AreNotEqual(new TypeAnnotations { Description = "a" }, new TypeAnnotations { Description = "b" });
            Assert.AreNotEqual(new TypeAnnotations { WriteOnly = true }, new TypeAnnotations { WriteOnly = false });
        }

        [TestMethod]
        public void RoundTripsKind()
        {
            var a = new TypeAnnotations { Kind = MeasuringPointKind.TotalIncreasing };
            Assert.AreEqual(MeasuringPointKind.TotalIncreasing, a.Kind);
        }

        [TestMethod]
        public void KindIsNullByDefault()
        {
            var a = new TypeAnnotations();
            Assert.IsNull(a.Kind);
        }

        [TestMethod]
        public void KindParticipatesInEquality()
        {
            Assert.AreNotEqual(new TypeAnnotations { Kind = MeasuringPointKind.Measurement }, new TypeAnnotations { Kind = MeasuringPointKind.TotalIncreasing });
        }
    }
}
