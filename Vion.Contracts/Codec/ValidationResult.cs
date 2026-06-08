using System.Collections.Immutable;

namespace Vion.Contracts.Codec
{
    /// <summary>
    ///     Outcome of a JSON-vs-schema validation pass — either <see cref="Valid" />, or invalid
    ///     with one or more error messages.
    /// </summary>
    public sealed record ValidationResult(bool IsValid, ImmutableArray<string> Errors)
    {
        /// <summary>The singleton result for a successful validation pass.</summary>
        public static readonly ValidationResult Valid = new(true, ImmutableArray<string>.Empty);

        /// <summary>Creates an invalid result carrying one or more diagnostic <paramref name="errors" />.</summary>
        public static ValidationResult Invalid(params string[] errors)
        {
            return new ValidationResult(false, errors.ToImmutableArray());
        }
    }
}
