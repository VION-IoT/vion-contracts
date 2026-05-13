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
    }
}
