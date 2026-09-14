using System.Collections.Generic;
using System.Text.Json.Nodes;
using Vion.Contracts.Predicates;

namespace Vion.Contracts.Test.Predicates
{
    /// <summary>
    ///     Focused tests for the strict-profile evaluator's fail-closed behavior — the cases the shared
    ///     conformance vector deliberately cannot pin (missing / null / type-mismatched references throw),
    ///     plus a representative slice of the dialect. The full core + strict vector is run by
    ///     <see cref="PredicateConformanceEvaluationShould" />.
    /// </summary>
    [TestClass]
    public class PredicateShould
    {
        [TestMethod]
        public void EvaluateABareBoolReference()
        {
            Assert.IsTrue(Predicate.Parse("ChargingEnabled").Evaluate(Ctx(("ChargingEnabled", JsonValue.Create(true)))));
            Assert.IsFalse(Predicate.Parse("ChargingEnabled").Evaluate(Ctx(("ChargingEnabled", JsonValue.Create(false)))));
        }

        [TestMethod]
        public void EvaluateIntegerComparisonsAndRelationals()
        {
            Assert.IsTrue(Predicate.Parse("ChargePointCount == 2").Evaluate(Ctx(("ChargePointCount", JsonValue.Create(2)))));
            Assert.IsFalse(Predicate.Parse("ChargePointCount == 2").Evaluate(Ctx(("ChargePointCount", JsonValue.Create(1)))));
            Assert.IsTrue(Predicate.Parse("ChargePointCount >= 2").Evaluate(Ctx(("ChargePointCount", JsonValue.Create(2)))));
            Assert.IsFalse(Predicate.Parse("ChargePointCount < 2").Evaluate(Ctx(("ChargePointCount", JsonValue.Create(2)))));
        }

        [TestMethod]
        public void EvaluateEnumAndStringEqualityCaseSensitively()
        {
            Assert.IsTrue(Predicate.Parse("Model == 'Cappuccino'").Evaluate(Ctx(("Model", JsonValue.Create("Cappuccino")))));
            Assert.IsFalse(Predicate.Parse("Model == 'Cappuccino'").Evaluate(Ctx(("Model", JsonValue.Create("cappuccino")))), "enum member names are case-sensitive");
            Assert.IsTrue(Predicate.Parse("Region != 'US'").Evaluate(Ctx(("Region", JsonValue.Create("EU")))));
        }

        [TestMethod]
        public void EvaluateMembershipForEnumAndIntegerReferences()
        {
            Assert.IsTrue(Predicate.Parse("Model in ['Moka', 'Ristretto', 'Cappuccino']").Evaluate(Ctx(("Model", JsonValue.Create("Ristretto")))));
            Assert.IsFalse(Predicate.Parse("Model in ['Moka', 'Ristretto']").Evaluate(Ctx(("Model", JsonValue.Create("Bricco")))));
            Assert.IsTrue(Predicate.Parse("ChargePointCount in [1, 2, 3]").Evaluate(Ctx(("ChargePointCount", JsonValue.Create(3)))));
        }

        [TestMethod]
        public void EvaluateBooleanCompositionAndParentheses()
        {
            var values = Ctx(("HasAcOutlet", JsonValue.Create(true)), ("Model", JsonValue.Create("Cappuccino")), ("ChargePointCount", JsonValue.Create(3)));
            Assert.IsTrue(Predicate.Parse("Model in ['Cappuccino', 'Corretto6'] && HasAcOutlet").Evaluate(values));
            Assert.IsTrue(Predicate.Parse("(ChargePointCount > 5 || Model == 'Cappuccino') && HasAcOutlet").Evaluate(values));
            Assert.IsFalse(Predicate.Parse("!HasAcOutlet").Evaluate(values));
        }

        [TestMethod]
        public void EvaluateAQualifiedReferenceByItsFullText()
        {
            // The context is a flat namespace keyed by the reference's full text — bare or two-segment.
            Assert.IsTrue(Predicate.Parse("ChargingStation.IsExternallyLocked == false").Evaluate(Ctx(("ChargingStation.IsExternallyLocked", JsonValue.Create(false)))));
        }

        [TestMethod]
        public void FailClosedWhenAReferenceIsMissing()
        {
            // Strict profile: an absent reference is a hard error, never a coerced false.
            Assert.Throws<PredicateEvaluationException>(() => Predicate.Parse("ChargePointCount >= 2").Evaluate(Ctx()));
        }

        [TestMethod]
        public void FailClosedWhenAReferenceValueIsNull()
        {
            Assert.Throws<PredicateEvaluationException>(() => Predicate.Parse("ChargePointCount >= 2").Evaluate(Ctx(("ChargePointCount", null))));
            Assert.Throws<PredicateEvaluationException>(() => Predicate.Parse("!Enabled").Evaluate(Ctx(("Enabled", null))));
        }

        [TestMethod]
        public void FailClosedWhenTheValueKindDoesNotMatchTheLiteral()
        {
            // An integer gate over a string-shaped value is a type mismatch, not a silent false.
            Assert.Throws<PredicateEvaluationException>(() => Predicate.Parse("ChargePointCount >= 2").Evaluate(Ctx(("ChargePointCount", JsonValue.Create("two")))));

            // A bare bool reference over a non-bool value likewise.
            Assert.Throws<PredicateEvaluationException>(() => Predicate.Parse("Enabled").Evaluate(Ctx(("Enabled", JsonValue.Create(1)))));
        }

        [TestMethod]
        public void ParseRejectsInputOutsideTheDialect()
        {
            Assert.Throws<PredicateSyntaxException>(() => Predicate.Parse("ChargePointCount * 2 == 4"));
            Assert.Throws<PredicateSyntaxException>(() => Predicate.Parse("Model == Cappuccino"), "unquoted enum member is rejected");
            Assert.Throws<PredicateSyntaxException>(() => Predicate.Parse("!ChargePointCount == 2"), "negation of a bare comparison is rejected");

            Assert.IsFalse(Predicate.TryParse("false == Enabled", out var predicate, out var error), "Yoda comparison is rejected");
            Assert.IsNull(predicate);
            Assert.IsFalse(string.IsNullOrWhiteSpace(error));
        }

        [TestMethod]
        public void ParseAcceptsAValidPredicate()
        {
            Assert.IsTrue(Predicate.TryParse("Model in ['Cappuccino', 'Corretto6'] && HasAcOutlet", out var predicate, out var error));
            Assert.IsNotNull(predicate);
            Assert.IsNull(error);
        }

        private static IReadOnlyDictionary<string, JsonNode?> Ctx(params (string Key, JsonNode? Value)[] pairs)
        {
            var dictionary = new Dictionary<string, JsonNode?>();
            foreach (var (key, value) in pairs)
            {
                dictionary[key] = value;
            }

            return dictionary;
        }
    }
}
