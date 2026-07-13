using System;

namespace Vion.Contracts.Predicates
{
    /// <summary>
    ///     Base type for the strict-profile predicate errors. A consumer that wants to treat any predicate
    ///     failure as a fail-closed configuration error (skip the gated member / fail activation) can catch
    ///     this single type; the two subtypes distinguish the phase the failure occurred in.
    /// </summary>
    public abstract class PredicateException : Exception
    {
        protected PredicateException(string message) : base(message)
        {
        }
    }

    /// <summary>
    ///     The predicate string is outside the dialect grammar (<c>docs/predicates.md</c>) and could not be
    ///     parsed. Thrown by <see cref="Predicate.Parse" />. In the strict profile an unparseable inclusion
    ///     gate is a hard configuration error — an analyzer-escaped producer bug — never silently treated as
    ///     "included".
    /// </summary>
    public sealed class PredicateSyntaxException : PredicateException
    {
        public PredicateSyntaxException(string message) : base(message)
        {
        }
    }

    /// <summary>
    ///     Evaluation could not produce a deterministic boolean: a referenced value is missing or JSON
    ///     <c>null</c>, or its JSON kind does not match the compared literal. Thrown by
    ///     <see cref="Predicate.Evaluate" />. The strict profile fails closed — an inclusion gate must be
    ///     deterministic, so an absent or type-mismatched reference is an error, never a coerced truthiness
    ///     result (contrast the UI profile in <c>docs/predicates.md</c>, which fails open).
    /// </summary>
    public sealed class PredicateEvaluationException : PredicateException
    {
        public PredicateEvaluationException(string message) : base(message)
        {
        }
    }
}
