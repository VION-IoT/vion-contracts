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
    ///     <see cref="ImmutableArray{T}" /> is reference-based. Guards against <c>default</c> arrays
    ///     (which are neither empty nor valid) prevent <see cref="System.NullReferenceException" /> in
    ///     deserialisation paths that construct records via reflection.
    /// </summary>
    public sealed record EnumTypeRef(
        string Title, // identity-bearing
        ImmutableArray<string> Members) : TypeRef
    {
        public bool Equals(EnumTypeRef? other)
        {
            if (other is null)
            {
                return false;
            }

            if (Title != other.Title)
            {
                return false;
            }

            if (Members.IsDefault || other.Members.IsDefault)
            {
                return false;
            }

            return Members.SequenceEqual(other.Members);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Title);
            if (!Members.IsDefault)
            {
                foreach (var m in Members)
                {
                    hash.Add(m);
                }
            }

            return hash.ToHashCode();
        }
    }

    /// <summary>
    ///     Nominal struct type. Identity is <c>(Title, Fields, Required)</c>. The custom
    ///     <see cref="Equals(StructTypeRef?)" /> performs element-wise comparison on the two
    ///     <see cref="ImmutableArray{T}" /> properties for the same reason as <see cref="EnumTypeRef" />,
    ///     and guards against <c>default</c> arrays to avoid <see cref="System.NullReferenceException" />.
    /// </summary>
    public sealed record StructTypeRef(
        string Title, // identity-bearing — mirrors EnumTypeRef
        ImmutableArray<StructField> Fields,
        ImmutableArray<string> Required) : TypeRef
    {
        public bool Equals(StructTypeRef? other)
        {
            if (other is null)
            {
                return false;
            }

            if (Title != other.Title)
            {
                return false;
            }

            if (Fields.IsDefault || other.Fields.IsDefault)
            {
                return false;
            }

            if (Required.IsDefault || other.Required.IsDefault)
            {
                return false;
            }

            return Fields.SequenceEqual(other.Fields) && Required.SequenceEqual(other.Required);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Title);
            if (!Fields.IsDefault)
            {
                foreach (var f in Fields)
                {
                    hash.Add(f);
                }
            }

            if (!Required.IsDefault)
            {
                foreach (var r in Required)
                {
                    hash.Add(r);
                }
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