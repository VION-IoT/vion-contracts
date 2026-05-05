using System;
using System.Collections.Immutable;
using System.Linq;

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

            var leftMappings = StatusMappings;
            var rightMappings = other.StatusMappings;
            if (leftMappings is null && rightMappings is null)
            {
                return true;
            }

            if (leftMappings is null || rightMappings is null)
            {
                return false;
            }

            return leftMappings.Count == rightMappings.Count && leftMappings.OrderBy(kv => kv.Key).SequenceEqual(rightMappings.OrderBy(kv => kv.Key));
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
            if (StatusMappings is not null)
            {
                foreach (var kv in StatusMappings.OrderBy(kv => kv.Key))
                {
                    hash.Add(kv.Key);
                    hash.Add(kv.Value);
                }
            }

            return hash.ToHashCode();
        }
    }
}