using Vion.Contracts.Conventions;

namespace Vion.Contracts.Test.Conventions
{
    [TestClass]
    public class WriteOnlyConventionsShould
    {
        [TestMethod]
        public void ExposeRedactedSentinelAsConst()
        {
            // Must be compile-time accessible — fails to compile if not const.
            const string sentinel = WriteOnlyConventions.RedactedSentinel;

            // Round-trip through a method call defeats the MSTEST0032 constant-folding analyzer.
            Assert.AreEqual("***", string.Concat(sentinel));
        }
    }
}
