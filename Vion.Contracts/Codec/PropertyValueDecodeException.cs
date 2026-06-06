using System;

namespace Vion.Contracts.Codec
{
    /// <summary>
    ///     Thrown when a FlatBuffers <c>PropertyValue</c> payload cannot be decoded — tag/schema
    ///     mismatch, malformed bytes, or out-of-range numeric narrowing.
    /// </summary>
    public sealed class PropertyValueDecodeException : Exception
    {
        public PropertyValueDecodeException(string message) : base(message)
        {
        }

        public PropertyValueDecodeException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
