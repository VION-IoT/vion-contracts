namespace Vion.Contracts.TypeRef
{
    /// <summary>
    ///     Dale-runtime behavior flags for a property. Advisory only at the codec layer.
    ///     Expanded as runtime needs emerge — currently <see cref="Persistent" /> and the effective
    ///     emission policy (<see cref="Throttle" />).
    /// </summary>
    public sealed record RuntimeMetadata
    {
        public static readonly RuntimeMetadata None = new();

        public bool Persistent { get; init; }

        /// <summary>
        ///     The effective emission policy (RFC 0004), populated by introspection only when it is
        ///     non-default; <c>null</c> otherwise.
        /// </summary>
        public ThrottleMetadata? Throttle { get; init; }

        public bool IsEmpty
        {
            get => !Persistent && Throttle is null;
        }
    }
}
