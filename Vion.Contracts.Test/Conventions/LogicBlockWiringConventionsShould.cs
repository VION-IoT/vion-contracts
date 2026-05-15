using Vion.Contracts.Conventions;

namespace Vion.Contracts.Test.Conventions
{
    [TestClass]
    public class LogicBlockWiringConventionsShould
    {
        // Change-detector: the dale-parser producer and the future cloud-api
        // consumer agree on these exact wire strings. A diff here is a wire break.

        [TestMethod]
        public void ExposeMultiplicityAnnotationKeyAsConst()
        {
            const string key = LogicBlockWiringConventions.MultiplicityAnnotationKey;

            Assert.AreEqual("Multiplicity", string.Concat(key));
        }

        [TestMethod]
        public void ExposeConsumersAnnotationKeyAsConst()
        {
            const string key = LogicBlockWiringConventions.ConsumersAnnotationKey;

            Assert.AreEqual("Consumers", string.Concat(key));
        }

        [TestMethod]
        public void ExposeTheFourLinkMultiplicityTokensAsConst()
        {
            const string exactlyOne = LogicBlockWiringConventions.ExactlyOne;
            const string zeroOrOne = LogicBlockWiringConventions.ZeroOrOne;
            const string oneOrMore = LogicBlockWiringConventions.OneOrMore;
            const string zeroOrMore = LogicBlockWiringConventions.ZeroOrMore;

            Assert.AreEqual("ExactlyOne", string.Concat(exactlyOne));
            Assert.AreEqual("ZeroOrOne", string.Concat(zeroOrOne));
            Assert.AreEqual("OneOrMore", string.Concat(oneOrMore));
            Assert.AreEqual("ZeroOrMore", string.Concat(zeroOrMore));
        }
    }
}
