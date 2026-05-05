using System;
using System.Collections.Immutable;
using System.Linq;

namespace Vion.Contracts.TypeRef
{
    public abstract record TypeRef;

    public sealed record PrimitiveTypeRef(PrimitiveKind Kind) : TypeRef;

    public sealed record EnumTypeRef(
        string Title, // identity-bearing
        ImmutableArray<string> Members) : TypeRef
    {
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

    public sealed record StructTypeRef(
        string Title, // identity-bearing — mirrors EnumTypeRef
        ImmutableArray<StructField> Fields,
        ImmutableArray<string> Required) : TypeRef
    {
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

    public sealed record ArrayTypeRef(TypeRef Items) : TypeRef;

    public sealed record NullableTypeRef(TypeRef Inner) : TypeRef;
}