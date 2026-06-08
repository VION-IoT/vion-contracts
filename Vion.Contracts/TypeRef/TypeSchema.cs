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
    ///     <para>
    ///         Custom <see cref="Equals(TypeSchema?)" /> and <see cref="GetHashCode" /> are required because
    ///         <see cref="ImmutableDictionary{TKey,TValue}" /> uses reference equality by default. The
    ///         constructor rejects null <see cref="StructFieldAnnotations" /> so invalid instances cannot
    ///         exist.
    ///     </para>
    ///     <para>
    ///         Note: the C# property <see cref="Type" /> does not appear under <c>"type"</c> in serialized
    ///         JSON Schema. Codec serialization (see TypeSchemaSerialization) emits the
    ///         <see cref="TypeRef" /> as a JSON Schema document where <c>"type"</c> is a JSON Schema
    ///         primitive-kind keyword (boolean / integer / number / string / array / object), not the
    ///         C# field name.
    ///     </para>
    /// </remarks>
    public sealed record TypeSchema(TypeRef Type, TypeAnnotations Annotations, ImmutableDictionary<string, TypeAnnotations> StructFieldAnnotations)
    {
        public ImmutableDictionary<string, TypeAnnotations> StructFieldAnnotations { get; init; } =
            StructFieldAnnotations ?? throw new ArgumentNullException(nameof(StructFieldAnnotations));

        /// <inheritdoc />
        public bool Equals(TypeSchema? other)
        {
            return other is not null && Type == other.Type && Annotations == other.Annotations &&
                   StructFieldAnnotations.OrderBy(kv => kv.Key).SequenceEqual(other.StructFieldAnnotations.OrderBy(kv => kv.Key));
        }

        /// <summary>
        ///     Convenience factory: creates a <see cref="TypeSchema" /> with no annotations.
        /// </summary>
        public static TypeSchema Of(TypeRef type)
        {
            return new TypeSchema(type, TypeAnnotations.None, ImmutableDictionary<string, TypeAnnotations>.Empty);
        }

        /// <summary>
        ///     Convenience factory: creates a <see cref="TypeSchema" /> with the given annotations and no
        ///     struct-field annotations — the common case for a primitive property or measuring point that
        ///     carries a unit, bounds, or a read/write-only flag.
        /// </summary>
        public static TypeSchema Of(TypeRef type, TypeAnnotations annotations)
        {
            return new TypeSchema(type, annotations, ImmutableDictionary<string, TypeAnnotations>.Empty);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(Type);
            h.Add(Annotations);
            foreach (var kv in StructFieldAnnotations.OrderBy(kv => kv.Key))
            {
                h.Add(kv.Key);
                h.Add(kv.Value);
            }

            return h.ToHashCode();
        }
    }
}
