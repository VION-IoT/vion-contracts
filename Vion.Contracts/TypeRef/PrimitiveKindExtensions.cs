using System;

namespace Vion.Contracts.TypeRef
{
    public static class PrimitiveKindExtensions
    {
        /// <summary>
        ///     Returns the canonical CLR type for a <see cref="PrimitiveKind" />, matching the widening used by
        ///     <c>PropertyValueCodec</c>: integer kinds collapse to <see cref="long" /> and number kinds to <see cref="double" />,
        ///     so consumers receive values without narrowing conversions at decode time.
        /// </summary>
        /// <param name="kind">The primitive kind.</param>
        /// <param name="isNullable">
        ///     When <c>true</c>, returns the nullable variant for value types. <see cref="string" /> is
        ///     unaffected.
        /// </param>
        public static Type ClrType(this PrimitiveKind kind, bool isNullable = false)
        {
            return kind switch
            {
                PrimitiveKind.Bool => isNullable ? typeof(bool?) : typeof(bool),
                PrimitiveKind.Byte or PrimitiveKind.Short or PrimitiveKind.UShort or PrimitiveKind.Int or PrimitiveKind.UInt or PrimitiveKind.Long =>
                    isNullable ? typeof(long?) : typeof(long),
                PrimitiveKind.Float or PrimitiveKind.Double => isNullable ? typeof(double?) : typeof(double),
                PrimitiveKind.String => typeof(string),
                PrimitiveKind.DateTime => isNullable ? typeof(DateTime?) : typeof(DateTime),
                PrimitiveKind.Duration => isNullable ? typeof(TimeSpan?) : typeof(TimeSpan),
                PrimitiveKind.Guid => isNullable ? typeof(Guid?) : typeof(Guid),
                _ => throw new NotSupportedException($"Unsupported primitive kind: {kind}"),
            };
        }
    }
}
