using System;

namespace Vion.Contracts.TypeRef
{
    /// <summary>
    ///     Thrown when JSON Schema input violates the Dale profile (e.g. unknown keyword, unsupported
    ///     <c>type</c> value, malformed enum). The parser direction (<c>FromJsonSchema</c>) raises
    ///     this; emit-only flows do not.
    /// </summary>
    public sealed class InvalidSchemaException : Exception
    {
        public InvalidSchemaException(string message) : base(message)
        {
        }

        public InvalidSchemaException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}