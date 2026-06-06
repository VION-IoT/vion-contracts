namespace Vion.Contracts.TypeRef
{
    /// <summary>
    ///     A single named field within a <see cref="StructTypeRef" />. The <see cref="Name" /> is
    ///     part of struct identity; renaming a field changes the struct's type fingerprint.
    /// </summary>
    public sealed record StructField(string Name, TypeRef Type);
}
