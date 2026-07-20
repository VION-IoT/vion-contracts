namespace Vion.Contracts.Mqtt
{
    /// <summary>
    ///     Public, ACL-limited bootstrap identity every service provider uses for the
    ///     pre-registration handshake. NOT a secret — NanoMQ restricts this user to the
    ///     registration topics only.
    /// </summary>
    public readonly record struct RegistrationCredentials(string Username, string Password)
    {
        public static RegistrationCredentials WellKnown { get; } = new("registration", "registration");
    }
}
