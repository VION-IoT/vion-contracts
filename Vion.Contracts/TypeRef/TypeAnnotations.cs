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

        public bool IsEmpty
        {
            get =>
                Title is null && Description is null && Unit is null
                && Minimum is null && Maximum is null && !ReadOnly;
        }
    }
}