namespace Vion.Contracts.TypeRef
{
    /// <summary>
    ///     The complete per-property metadata document: the data-shape <see cref="TypeSchema" />,
    ///     UI-side <see cref="Presentation" /> hints, and Dale-runtime <see cref="RuntimeMetadata" />
    ///     flags. Emitted by introspection as three sibling JSON documents.
    /// </summary>
    public sealed record PropertyMetadata(TypeSchema Schema, Presentation Presentation, RuntimeMetadata Runtime)
    {
        /// <summary>
        ///     Convenience factory: creates a <see cref="PropertyMetadata" /> with no presentation hints
        ///     and no runtime flags.
        /// </summary>
        public static PropertyMetadata Of(TypeSchema schema) => new(schema, Presentation.None, RuntimeMetadata.None);
    }
}
