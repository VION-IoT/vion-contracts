using Vion.Contracts.Constants;

namespace Vion.Contracts.Test.Constants
{
    [TestClass]
    public class RemoteAccessConstantsShould
    {
        // Change-detector: these strings leave this repo. The service names are the MQTT topic's final
        // segment AND the gateway wrapper-service / session-unit names (mirrored in the hardware-integration
        // shell allowlists); the argument names are the camelCase wire keys the gateway env-key allowlist
        // filters on. A diff here is a wire break, not a rename.

        [TestMethod]
        public void ExposeTheThreeSessionProfileServiceNamesAsConst()
        {
            const string remoteVpn = RemoteAccessConstants.Services.RemoteVpn;
            const string remoteConsole = RemoteAccessConstants.Services.RemoteConsole;
            const string remoteTwincat = RemoteAccessConstants.Services.RemoteTwincat;

            Assert.AreEqual("remote-vpn", string.Concat(remoteVpn));
            Assert.AreEqual("remote-console", string.Concat(remoteConsole));
            Assert.AreEqual("remote-twincat", string.Concat(remoteTwincat));
        }

        [TestMethod]
        public void ExposeTheVpnProfileArgumentNamesAsConst()
        {
            const string loginServerUrl = RemoteAccessConstants.Arguments.LoginServerUrl;
            const string ephemeralAuthKey = RemoteAccessConstants.Arguments.EphemeralAuthKey;

            Assert.AreEqual("loginServerUrl", string.Concat(loginServerUrl));
            Assert.AreEqual("ephemeralAuthKey", string.Concat(ephemeralAuthKey));
        }

        [TestMethod]
        public void ExposeTheConsoleProfileRelayArgumentNamesAsConst()
        {
            const string relayEndpoint = RemoteAccessConstants.Arguments.RelayEndpoint;
            const string relaySessionToken = RemoteAccessConstants.Arguments.RelaySessionToken;
            const string relayHostKey = RemoteAccessConstants.Arguments.RelayHostKey;

            Assert.AreEqual("relayEndpoint", string.Concat(relayEndpoint));
            Assert.AreEqual("relaySessionToken", string.Concat(relaySessionToken));
            Assert.AreEqual("relayHostKey", string.Concat(relayHostKey));
        }

        [TestMethod]
        public void ExposeTheProfileAgnosticExpiryArgumentNameAsConst()
        {
            const string expiresAtUtc = RemoteAccessConstants.Arguments.ExpiresAtUtc;

            Assert.AreEqual("expiresAtUtc", string.Concat(expiresAtUtc));
        }
    }
}
