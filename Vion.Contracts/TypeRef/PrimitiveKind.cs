namespace Vion.Contracts.TypeRef
{
    /// <summary>
    ///     Scalar primitive kinds supported by the type system. Each value maps 1-to-1 to a wire codec
    ///     and a corresponding CLR type; the mapping is exhaustive and stable across versions.
    /// </summary>
    public enum PrimitiveKind
    {
        Bool,

        String,

        Byte,

        Short,

        UShort,

        Int,

        UInt,

        Long,

        Float,

        Double,

        DateTime,

        Duration,
    }
}