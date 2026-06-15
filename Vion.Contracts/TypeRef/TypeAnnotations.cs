namespace Vion.Contracts.TypeRef
{
    /// <summary>
    ///     JSON Schema annotations that attach to a <see cref="TypeRef" />. Not identity-bearing —
    ///     two <see cref="TypeSchema" /> instances with the same <see cref="TypeRef" /> but different
    ///     annotations are the same type for codec purposes; they may render or validate differently.
    /// </summary>
    public sealed record TypeAnnotations
    {
        public static readonly TypeAnnotations None = new();

        public string? Title { get; init; }

        public string? Description { get; init; }

        public string? Unit { get; init; }

        public double? Minimum { get; init; }

        public double? Maximum { get; init; }

        public bool ReadOnly { get; init; }

        public bool WriteOnly { get; init; }

        public MeasuringPointKind? Kind { get; init; }

        /// <summary>
        ///     JSON Schema <c>format</c> for a string value whose shape is author-declared (e.g.
        ///     <c>"ipv4"</c>). Advisory — consumers may render/soft-validate, the codec never rejects.
        ///     NOT set for type-derived formats (<c>date-time</c>/<c>duration</c>/<c>uuid</c>); those
        ///     are <see cref="PrimitiveKind" /> values, not annotations.
        /// </summary>
        public string? Format { get; init; }

        public bool IsEmpty
        {
            get => Title is null && Description is null && Unit is null && Minimum is null && Maximum is null && !ReadOnly && !WriteOnly && Kind is null && Format is null;
        }
    }
}
