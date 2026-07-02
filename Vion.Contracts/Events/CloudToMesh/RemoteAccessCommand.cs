using System;

namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     Whether a remote-access command opens or closes the session on the gateway.
    /// </summary>
    public enum RemoteAccessAction
    {
        Start = 0,

        Stop,
    }

    /// <summary>
    ///     Parameters the gateway needs to bring a remote-access session up. Present on
    ///     <see cref="RemoteAccessAction.Start" />; null on <see cref="RemoteAccessAction.Stop" />.
    /// </summary>
    /// <param name="LoginServerUrl">The session's dedicated Headscale control-server URL (HTTPS).</param>
    /// <param name="EphemeralAuthKey">Single-use, TTL-bound pre-auth key for the session tailnet.</param>
    /// <param name="ExpiresAtUtc">
    ///     Hard local expiry. The gateway tears the session down at this time even if the stop command never
    ///     arrives, so a lost teardown fails closed rather than leaving a tunnel open.
    /// </param>
    public record RemoteAccessSessionParameters(string LoginServerUrl, string EphemeralAuthKey, DateTimeOffset ExpiresAtUtc);
}
