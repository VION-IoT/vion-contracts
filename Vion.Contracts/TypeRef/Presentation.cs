using System.Collections.Immutable;

namespace Vion.Contracts.TypeRef
{
    /// <summary>
    ///     UI hints for a property — display name override, grouping, ordering, severity mappings.
    ///     Advisory only: codec and Mesh ignore these. Cloud + dashboard consume them when rendering.
    /// </summary>
    public sealed record Presentation
    {
        public static readonly Presentation None = new();

        public string? DisplayName { get; init; }

        public string? Group { get; init; }

        public int? Order { get; init; }

        public string? Category { get; init; }

        public string? Importance { get; init; }

        public string? UIHint { get; init; }

        public int? Decimals { get; init; }

        public ImmutableDictionary<string, string>? StatusMappings { get; init; }

        public bool IsEmpty
        {
            get =>
                DisplayName is null && Group is null && Order is null && Category is null && Importance is null && UIHint is null && Decimals is null &&
                (StatusMappings is null || StatusMappings.IsEmpty);
        }
    }
}