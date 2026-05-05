using System;
using System.Collections.Immutable;
using System.Linq;

namespace Vion.Contracts.TypeRef
{
    /// <summary>
    ///     Discriminated union of all first-class type references. Equality is structural and drives
    ///     codec dispatch, registry deduplication, and schema identity checks throughout the platform.
    /// </summary>
    public abstract record TypeRef;

    /// <summary>
    ///     Wraps a built-in scalar kind. Record equality is sufficient because <see cref="PrimitiveKind" />
    ///     is an enum — no custom <c>Equals</c> needed.
    /// </summary>
    public sealed record PrimitiveTypeRef(PrimitiveKind Kind) : TypeRef;

    /// <summary>
    ///     Nominal enum type. Identity is <c>(Title, Members)</c> — renaming an enum produces a different
    ///     type even with the same members. The custom <see cref="Equals(EnumTypeRef?)" /> uses element-wise
    ///     comparison on <see cref="Members" /> because default record equality on
    ///     <see cref="ImmutableArray{T}" /> is reference-based. The constructor rejects
    ///     <c>default(ImmutableArray&lt;string&gt;)</c> so invalid instances cannot exist.
    /// </summary>
    public sealed record EnumTypeRef(
        string Title, // identity-bearing
        ImmutableArray<string> Members) : TypeRef
    {
        public ImmutableArray<string> Members { get; init; } = !Members.IsDefault ? Members :
                                                                   throw new ArgumentException("Members must be initialized; default(ImmutableArray<string>) is not a valid value.",
                                                                                               nameof(Members));

        public bool Equals(EnumTypeRef? other)
        {
            return other is not null && Title == other.Title && Members.SequenceEqual(other.Members);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Title);
            foreach (var m in Members)
            {
                hash.Add(m);
            }

            return hash.ToHashCode();
        }
    }

    /// <summary>
    ///     Nominal struct type. Identity is <c>(Title, Fields, Required)</c>. The custom
    ///     <see cref="Equals(StructTypeRef?)" /> performs element-wise comparison on the two
    ///     <see cref="ImmutableArray{T}" /> properties for the same reason as <see cref="EnumTypeRef" />.
    ///     The constructor rejects <c>default</c> arrays so invalid instances cannot exist.
    /// </summary>
    public sealed record StructTypeRef(
        string Title, // identity-bearing — mirrors EnumTypeRef
        ImmutableArray<StructField> Fields,
        ImmutableArray<string> Required) : TypeRef
    {
        public ImmutableArray<StructField> Fields { get; init; } = !Fields.IsDefault ? Fields :
                                                                       throw new
                                                                           ArgumentException("Fields must be initialized; default(ImmutableArray<StructField>) is not a valid value.",
                                                                                             nameof(Fields));

        public ImmutableArray<string> Required { get; init; } = !Required.IsDefault ? Required :
                                                                    throw new
                                                                        ArgumentException("Required must be initialized; default(ImmutableArray<string>) is not a valid value.",
                                                                                          nameof(Required));

        public bool Equals(StructTypeRef? other)
        {
            return other is not null && Title == other.Title && Fields.SequenceEqual(other.Fields) && Required.SequenceEqual(other.Required);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Title);
            foreach (var f in Fields)
            {
                hash.Add(f);
            }

            foreach (var r in Required)
            {
                hash.Add(r);
            }

            return hash.ToHashCode();
        }
    }

    /// <summary>
    ///     Array of a homogeneous item type. Identity follows from <see cref="Items" /> identity.
    /// </summary>
    public sealed record ArrayTypeRef(TypeRef Items) : TypeRef;

    /// <summary>
    ///     Nullable wrapper. Identity follows from <see cref="Inner" /> identity.
    /// </summary>
    public sealed record NullableTypeRef(TypeRef Inner) : TypeRef;
}