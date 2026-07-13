using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Vion.Contracts.Predicates;

namespace Vion.Contracts.Test.Predicates
{
    /// <summary>
    ///     Runs the shared conformance vector (<c>Vion.Contracts/Predicates/predicate-conformance.json</c>)
    ///     through the strict-profile <see cref="Predicate" /> evaluator this package now ships. Every core
    ///     (untagged) case and every <c>"profile": "strict"</c> positive case must evaluate to its
    ///     <c>expected</c> boolean; every strict error case and every UI-profile (null-binding) case must fail
    ///     closed. This is the drift guard the vector exists for — the SDK and cloud-api run the same cases.
    /// </summary>
    [TestClass]
    public class PredicateConformanceEvaluationShould
    {
        [TestMethod]
        public void EvaluateEveryCoreAndStrictPositiveCaseToItsExpectedResult()
        {
            var count = 0;
            foreach (var c in EvalCases())
            {
                if (IsError(c) || Profile(c) == "ui")
                {
                    continue; // fail-closed cases are asserted separately
                }

                var name = c["name"]!.GetValue<string>();
                var predicate = Predicate.Parse(c["predicate"]!.GetValue<string>());
                var expected = c["expected"]!.GetValue<bool>();

                Assert.AreEqual(expected, predicate.Evaluate(ContextOf(c)), $"eval case '{name}' did not match its expected result");
                count++;
            }

            Assert.IsGreaterThan(20, count, "expected the vector to exercise a substantial set of core/strict positive eval cases");
        }

        [TestMethod]
        public void FailClosedOnEveryStrictErrorCase()
        {
            var count = 0;
            foreach (var c in EvalCases().Where(IsError))
            {
                var name = c["name"]!.GetValue<string>();
                var predicate = Predicate.Parse(c["predicate"]!.GetValue<string>());

                Assert.Throws<PredicateEvaluationException>(() => predicate.Evaluate(ContextOf(c)), $"strict error case '{name}' must fail closed");
                count++;
            }

            Assert.IsGreaterThan(0, count, "expected at least one strict error case in the vector");
        }

        [TestMethod]
        public void FailClosedOnEveryUiProfileCaseBecauseStrictNeverFacesNull()
        {
            // Every UI-profile case binds a ref to null; the strict evaluator, which is deterministic by
            // construction, rejects that rather than coercing a truthiness result.
            var count = 0;
            foreach (var c in EvalCases().Where(c => Profile(c) == "ui"))
            {
                var name = c["name"]!.GetValue<string>();
                var predicate = Predicate.Parse(c["predicate"]!.GetValue<string>());

                Assert.Throws<PredicateEvaluationException>(() => predicate.Evaluate(ContextOf(c)), $"UI-profile case '{name}' must fail closed under the strict evaluator");
                count++;
            }

            Assert.IsGreaterThan(0, count, "expected at least one UI-profile case in the vector");
        }

        [TestMethod]
        public void ParseEveryVectorParseCaseToItsValidityFlag()
        {
            var count = 0;
            foreach (var node in LoadVectorRoot()["parse"]!.AsArray())
            {
                var c = node!.AsObject();
                var name = c["name"]!.GetValue<string>();
                var valid = c["valid"]!.GetValue<bool>();

                Assert.AreEqual(valid, Predicate.TryParse(c["predicate"]!.GetValue<string>(), out _, out _), $"parse case '{name}' disagreed with the vector");
                count++;
            }

            Assert.IsGreaterThan(0, count);
        }

        private static IEnumerable<JsonObject> EvalCases()
        {
            return LoadVectorRoot()["eval"]!.AsArray().Select(node => node!.AsObject());
        }

        private static bool IsError(JsonObject c)
        {
            return c["error"] is not null && c["error"]!.GetValue<bool>();
        }

        private static string? Profile(JsonObject c)
        {
            return c["profile"]?.GetValue<string>();
        }

        private static IReadOnlyDictionary<string, JsonNode?> ContextOf(JsonObject c)
        {
            var dictionary = new Dictionary<string, JsonNode?>();
            foreach (var (key, value) in c["values"]!.AsObject())
            {
                dictionary[key] = value?.DeepClone();
            }

            return dictionary;
        }

        private static JsonObject LoadVectorRoot()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Predicates", "predicate-conformance.json");
            Assert.IsTrue(File.Exists(path), $"conformance vector not found at {path}");
            var root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
            Assert.IsNotNull(root, "conformance vector must be a JSON object");
            return root!;
        }
    }
}
