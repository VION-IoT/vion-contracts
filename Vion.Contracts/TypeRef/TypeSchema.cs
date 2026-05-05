using System;
using System.Collections.Immutable;
using System.Linq;

namespace Vion.Contracts.TypeRef
{
    /// <summary>
    ///     The complete type description for a property: the structural <see cref="TypeRef" /> identity
    ///     plus the annotations that attach to it. Identity vs annotation split is intentional: two
    ///     <see cref="TypeSchema" /> instances with the same <see cref="Type" /> but different
    ///     <see cref="Annotations" /> are the same type for codec purposes, and may render or validate
    ///     differently.
    /// </summary>
    /// <remarks>
    ///     Custom <see cref="Equals(TypeSchema?)" /> and <see cref="GetHashCode" /> are required because
    ///     <see cref="ImmutableDictionary{TKey, TValue}" /> uses reference equality by default. Null
    ///     <see cref="StructFieldAnnotations" /> is treated as "unknown" — not equal to any other instance.
    /// </remarks>
    public sealed record TypeSchema(TypeRef Type, TypeAnnotations Annotations, ImmutableDictionary<string, TypeAnnotations> StructFieldAnnotations)
    {
        /// <summary>
        ///     Convenience factory: creates a <see cref="TypeSchema" /> with no annotations.
        /// </summary>
        public static TypeSchema Of(TypeRef type) => new(type, TypeAnnotations.None, ImmutableDictionary<string, TypeAnnotations>.Empty);

        /// <inheritdoc />
        public bool Equals(TypeSchema? other)
        {
            if (other is null)
            {
                return false;
            }

            if (Type != other.Type)
            {
                return false;
            }

            if (Annotations != other.Annotations)
            {
                return false;
            }

            if (StructFieldAnnotations is null || other.StructFieldAnnotations is null)
            {
                return false;
            }

            return StructFieldAnnotations.OrderBy(kv => kv.Key).SequenceEqual(other.StructFieldAnnotations.OrderBy(kv => kv.Key));
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(Type);
            h.Add(Annotations);
            if (StructFieldAnnotations is not null)
            {
                foreach (var kv in StructFieldAnnotations.OrderBy(kv => kv.Key))
                {
                    h.Add(kv.Key);
                    h.Add(kv.Value);
                }
            }

            return h.ToHashCode();
        }
    }
}
