using Vion.Contracts.Conventions;

namespace Vion.Contracts.Test.Conventions
{
    [TestClass]
    public class DaleSdkCompatibilityConventionsShould
    {
        // Change-detector: dale, dale-sdk and cloud-api agree on these exact annotation keys.
        // A diff here is a wire break.

        [TestMethod]
        public void ExposeVersionAnnotationKeyAsConst()
        {
            const string key = ProviderAnnotationKeys.VersionAnnotationKey;

            Assert.AreEqual("Version", string.Concat(key));
        }

        [TestMethod]
        public void ExposeDaleSdkVersionAnnotationKeyAsConst()
        {
            const string key = DaleSdkCompatibilityConventions.DaleSdkVersionAnnotationKey;

            Assert.AreEqual("DaleSdkVersion", string.Concat(key));
        }

        [TestMethod]
        public void ExposeTargetDaleSdkVersionAnnotationKeyAsConst()
        {
            const string key = DaleSdkCompatibilityConventions.TargetDaleSdkVersionAnnotationKey;

            Assert.AreEqual("TargetDaleSdkVersion", string.Concat(key));
        }

        [TestMethod]
        public void ExposeMinimumDaleSdkVersionAnnotationKeyAsConst()
        {
            // Also the MSBuild property and AssemblyMetadata key in dale-sdk; must not be renamed.
            const string key = DaleSdkCompatibilityConventions.MinimumDaleSdkVersionAnnotationKey;

            Assert.AreEqual("MinimumDaleSdkVersion", string.Concat(key));
        }
    }
}
