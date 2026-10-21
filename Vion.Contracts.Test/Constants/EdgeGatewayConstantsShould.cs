using Vion.Contracts.Constants;

namespace Vion.Contracts.Test.Constants
{
    [TestClass]
    public class EdgeGatewayConstantsShould
    {
        // Change-detector: device certificates are issued and checked against this prefix in other repos.
        // A diff here is a wire break, not a rename.

        [TestMethod]
        public void ExposeTheCertificateCommonNamePrefixAsConst()
        {
            const string prefix = EdgeGatewayConstants.CertificateCommonNamePrefix;

            Assert.AreEqual("urn:vion:gateway:", string.Concat(prefix));
        }
    }
}
