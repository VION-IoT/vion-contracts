namespace Vion.Contracts.TypeRef
{
    /// <summary>
    ///     Dale-runtime behavior flags for a property. Advisory only at the codec layer.
    ///     Currently just <see cref="Persistent" />; expanded as runtime needs emerge.
    /// </summary>
    public sealed record RuntimeMetadata
    {
        public static readonly RuntimeMetadata None = new();

        public bool Persistent { get; init; }

        public bool IsEmpty
        {
            get => !Persistent;
        }
    }
}
