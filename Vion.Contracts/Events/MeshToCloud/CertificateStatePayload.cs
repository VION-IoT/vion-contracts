using System;

namespace Vion.Contracts.Events.MeshToCloud
{
    /// <summary>
    ///     Represents the state of an edge gateway's device certificate.
    /// </summary>
    /// <param name="NotBefore">
    ///     The UTC start of the certificate's validity window (the X.509 certificate's <c>NotBefore</c>).
    /// </param>
    /// <param name="NotAfter">
    ///     The UTC end of the certificate's validity window (the X.509 certificate's <c>NotAfter</c>), i.e. when it expires.
    /// </param>
    [Schema("CertificateStatePayload")]
    public record CertificateStatePayload(DateTime NotBefore, DateTime NotAfter) : IMessage;
}
