namespace Vion.Contracts.TypeRef
{
    /// <summary>
    ///     The effective emission policy (RFC 0004 — throttle / deadband / dedup) configured for a
    ///     property, surfaced so dashboards and the DevHost can display the configured rate control.
    ///     Advisory only at the codec layer. Carried on <see cref="RuntimeMetadata.Throttle" /> and
    ///     populated by the SDK introspection only when the policy is non-default.
    /// </summary>
    public sealed record ThrottleMetadata
    {
        /// <summary>
        ///     Minimum spacing between emissions, as a duration string (e.g. <c>"250ms"</c>, <c>"1s"</c>);
        ///     <c>"0"</c> / <c>"0ms"</c> means throttling is disabled.
        /// </summary>
        public string MinInterval { get; init; } = "250ms";

        /// <summary>
        ///     The change-threshold (deadband) string, interpreted by the value type's change logic;
        ///     <c>null</c> when no deadband is configured (only the value-equality dedup floor runs).
        /// </summary>
        public string? MinChange { get; init; }

        /// <summary>
        ///     When <c>true</c>, every change is emitted — throttle and deadband are bypassed.
        /// </summary>
        public bool Immediate { get; init; }
    }
}
