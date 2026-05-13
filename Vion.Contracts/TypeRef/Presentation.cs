using System;
using System.Collections.Immutable;
using System.Linq;

namespace Vion.Contracts.TypeRef
{
    /// <summary>
    ///     UI hints for a property — display name override, grouping, ordering, severity mappings,
    ///     enum-member display labels.
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

        /// <summary>
        ///     Renderer-facing format-token string for date / duration / numeric formatting.
        ///     Type-orthogonal — orthogonal to <see cref="UIHint" /> (which carries the
        ///     widget-routing key, e.g. <c>"trigger"</c>, <c>"sparkline"</c>). When set, the
        ///     dashboard's date library (moment.js / day.js / Luxon) consumes it directly for
        ///     <see cref="System.DateTime" /> or <see cref="System.TimeSpan" /> properties.
        ///     Two reserved sentinel values short-circuit the token interpreter:
        ///     <c>"relative"</c> for auto-updating <c>"3 minutes ago"</c>-style date display,
        ///     and <c>"humanize"</c> for a humanized duration like <c>"3 hours"</c>.
        /// </summary>
        public string? Format { get; init; }

        public ImmutableDictionary<string, string>? StatusMappings { get; init; }

        /// <summary>
        ///     Display labels per enum member name, when the property's schema is an enum.
        ///     Populated from <c>[EnumValueInfo("...")]</c> on enum members. Map key is the CLR
        ///     member name (matches the wire value per spec §5.4); map value is the human-readable
        ///     label the dashboard should show. Members without a display label are omitted from
        ///     the map; consumers fall back to the member name when no label is present.
        /// </summary>
        public ImmutableDictionary<string, string>? EnumLabels { get; init; }

        public bool IsEmpty
        {
            get =>
                DisplayName is null && Group is null && Order is null && Category is null && Importance is null && UIHint is null && Decimals is null && Format is null &&
                (StatusMappings is null || StatusMappings.IsEmpty) && (EnumLabels is null || EnumLabels.IsEmpty);
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Custom equality required: <see cref="ImmutableDictionary{TKey,TValue}" /> uses reference equality
        ///     by default, which breaks round-trip comparisons after serialization. Mirrors the pattern in
        ///     <see cref="TypeSchema" />.
        /// </remarks>
        public bool Equals(Presentation? other)
        {
            if (other is null)
            {
                return false;
            }

            if (DisplayName != other.DisplayName)
            {
                return false;
            }

            if (Group != other.Group)
            {
                return false;
            }

            if (Order != other.Order)
            {
                return false;
            }

            if (Category != other.Category)
            {
                return false;
            }

            if (Importance != other.Importance)
            {
                return false;
            }

            if (UIHint != other.UIHint)
            {
                return false;
            }

            if (Decimals != other.Decimals)
            {
                return false;
            }

            if (Format != other.Format)
            {
                return false;
            }

            if (!DictionariesEqual(StatusMappings, other.StatusMappings))
            {
                return false;
            }

            if (!DictionariesEqual(EnumLabels, other.EnumLabels))
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(DisplayName);
            hash.Add(Group);
            hash.Add(Order);
            hash.Add(Category);
            hash.Add(Importance);
            hash.Add(UIHint);
            hash.Add(Decimals);
            hash.Add(Format);
            AddDictionary(ref hash, StatusMappings);
            AddDictionary(ref hash, EnumLabels);
            return hash.ToHashCode();
        }

        private static bool DictionariesEqual(ImmutableDictionary<string, string>? left, ImmutableDictionary<string, string>? right)
        {
            if (left is null && right is null)
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return left.Count == right.Count && left.OrderBy(kv => kv.Key).SequenceEqual(right.OrderBy(kv => kv.Key));
        }

        private static void AddDictionary(ref HashCode hash, ImmutableDictionary<string, string>? dict)
        {
            if (dict is null)
            {
                return;
            }

            foreach (var kv in dict.OrderBy(kv => kv.Key))
            {
                hash.Add(kv.Key);
                hash.Add(kv.Value);
            }
        }
    }
}
