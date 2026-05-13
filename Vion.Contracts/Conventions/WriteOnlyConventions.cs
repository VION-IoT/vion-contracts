namespace Vion.Contracts.Conventions
{
    /// <summary>
    ///     Wire-level conventions for writeOnly properties (JSON Schema 2020-12 writeOnly keyword).
    /// </summary>
    public static class WriteOnlyConventions
    {
        /// <summary>
        ///     The literal string Dale publishes on /sw/property/state for any
        ///     writeOnly string property whose value is currently set (i.e. non-null).
        ///     The dashboard reads this in combination with schema.writeOnly === true
        ///     to render the "set, hidden" state. A null value → payload = NONE;
        ///     a never-set value → no retained message.
        /// </summary>
        public const string RedactedSentinel = "***";
    }
}
