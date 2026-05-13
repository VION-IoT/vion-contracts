using Vion.Contracts.TypeRef;

namespace Vion.Contracts.Test.TypeRef
{
    [TestClass]
    public class PresentationShould
    {
        [TestMethod]
        public void RoundTripsOrderField()
        {
            var p = new Presentation { Order = -1 };
            Assert.AreEqual(-1, p.Order);
            Assert.IsFalse(p.IsEmpty);
        }

        [TestMethod]
        public void OrderIsNullByDefault()
        {
            var p = new Presentation();
            Assert.IsNull(p.Order);
        }

        [TestMethod]
        public void OrderParticipatesInEquality()
        {
            var p1 = new Presentation { Order = 1 };
            var p2 = new Presentation { Order = 1 };
            var p3 = new Presentation { Order = 2 };
            Assert.AreEqual(p1, p2);
            Assert.AreNotEqual(p1, p3);
        }

        [TestMethod]
        public void RoundTripFormatField()
        {
            var p = new Presentation { Format = "LLLL" };
            Assert.AreEqual("LLLL", p.Format);
            Assert.IsFalse(p.IsEmpty);
        }

        [TestMethod]
        public void FormatIsNullByDefault()
        {
            var p = new Presentation();
            Assert.IsNull(p.Format);
        }

        [TestMethod]
        public void FormatParticipatesInEquality()
        {
            var p1 = new Presentation { Format = "LLLL" };
            var p2 = new Presentation { Format = "LLLL" };
            var p3 = new Presentation { Format = "YYYY-MM-DD" };
            Assert.AreEqual(p1, p2);
            Assert.AreNotEqual(p1, p3);
        }

        [TestMethod]
        public void FormatParticipatesInHashCode()
        {
            var p1 = new Presentation { Format = "LLLL" };
            var p2 = new Presentation { Format = "LLLL" };
            var p3 = new Presentation { Format = "YYYY-MM-DD" };
            Assert.AreEqual(p1.GetHashCode(), p2.GetHashCode());
            Assert.AreNotEqual(p1.GetHashCode(), p3.GetHashCode());
        }
    }
}
