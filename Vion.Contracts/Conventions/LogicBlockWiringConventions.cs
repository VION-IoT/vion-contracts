namespace Vion.Contracts.Conventions
{
    /// <summary>
    ///     Wire-level conventions for logic-block contract-link multiplicity, carried
    ///     in the loose <c>Annotations</c> dictionary of
    ///     <see cref="Vion.Contracts.Introspection.LogicBlockIntrospectionResult" />
    ///     (the dale-parser ↔ cloud-api contract).
    ///     <para>
    ///         The <c>LinkMultiplicity</c> enum itself stays SDK-owned
    ///         (<c>Vion.Dale.Sdk.Core</c>); vion-contracts holds only the annotation
    ///         key names and the four token strings, so the dale-parser producer and
    ///         the (future) cloud-api consumer agree on the exact wire vocabulary —
    ///         mirroring how <see cref="WriteOnlyConventions" /> owns the sentinel
    ///         string without owning the SDK attribute.
    ///     </para>
    /// </summary>
    public static class LogicBlockWiringConventions
    {
        /// <summary>
        ///     Consumer-side annotation key. Present in
        ///     <c>LogicBlockIntrospectionResult.InterfaceInfo.Annotations</c> /
        ///     <c>ContractInfo.Annotations</c> when a binding declares a non-default
        ///     link multiplicity; the value is one of the four token strings below.
        /// </summary>
        public const string MultiplicityAnnotationKey = "Multiplicity";

        /// <summary>
        ///     Provider-side annotation key. Present in
        ///     <c>LogicBlockIntrospectionResult.ContractInfo.Annotations</c> when a
        ///     provided contract role caps how many consumers it accepts; the value
        ///     is one of the four token strings below.
        /// </summary>
        public const string ConsumersAnnotationKey = "Consumers";

        /// <summary>Token for <c>LinkMultiplicity.ExactlyOne</c> (required, single).</summary>
        public const string ExactlyOne = "ExactlyOne";

        /// <summary>Token for <c>LinkMultiplicity.ZeroOrOne</c> (optional, single).</summary>
        public const string ZeroOrOne = "ZeroOrOne";

        /// <summary>Token for <c>LinkMultiplicity.OneOrMore</c> (required, many).</summary>
        public const string OneOrMore = "OneOrMore";

        /// <summary>Token for <c>LinkMultiplicity.ZeroOrMore</c> (optional, many — the unconstrained default).</summary>
        public const string ZeroOrMore = "ZeroOrMore";
    }
}
