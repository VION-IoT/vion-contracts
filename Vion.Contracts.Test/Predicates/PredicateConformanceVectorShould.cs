using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace Vion.Contracts.Test.Predicates
{
    /// <summary>
    ///     Guards the shared predicate conformance vector
    ///     (<c>Vion.Contracts/Predicates/predicate-conformance.json</c>): it must stay valid JSON
    ///     that System.Text.Json (and, downstream, JSON.parse) reads, and every case must carry the
    ///     fields consumers dispatch on. This package hosts the vector but ships no evaluator, so the
    ///     guard asserts <em>shape</em>, not evaluation — the dashboard / DevHost run the eval cases,
    ///     the SDK analyzer runs the parse cases (see <c>docs/predicates.md</c>).
    /// </summary>
    [TestClass]
    public class PredicateConformanceVectorShould
    {
        [TestMethod]
        public void ParseAsJsonWithNonEmptyEvalAndParseLists()
        {
            var root = LoadVectorRoot();
            Assert.IsInstanceOfType<JsonArray>(root["eval"], "vector must have an 'eval' array");
            Assert.IsInstanceOfType<JsonArray>(root["parse"], "vector must have a 'parse' array");
            Assert.IsTrue(root["eval"]!.AsArray().Any(), "'eval' must not be empty");
            Assert.IsTrue(root["parse"]!.AsArray().Any(), "'parse' must not be empty");
        }

        [TestMethod]
        public void GiveEveryEvalCaseNamePredicateValuesAndAnOutcome()
        {
            foreach (var node in LoadVectorRoot()["eval"]!.AsArray())
            {
                var c = node!.AsObject();
                Assert.IsFalse(string.IsNullOrWhiteSpace(c["name"]?.GetValue<string>()), "eval case is missing 'name'");
                Assert.IsFalse(string.IsNullOrWhiteSpace(c["predicate"]?.GetValue<string>()), $"eval case '{c["name"]}' is missing 'predicate'");
                Assert.IsInstanceOfType<JsonObject>(c["values"], $"eval case '{c["name"]}' is missing a 'values' object");

                // Outcome is either a boolean 'expected' (the consumer truthiness-tests it into visible/hidden,
                // or — strict profile — takes it as the resolved gate) OR "error": true for a strict
                // fail-closed case, which carries no 'expected'. Exactly one must be present.
                var isError = c["error"] is not null && c["error"]!.GetValue<bool>();
                if (isError)
                {
                    Assert.IsNull(c["expected"], $"error case '{c["name"]}' must not also carry 'expected'");
                    Assert.AreEqual("strict", c["profile"]?.GetValue<string>(), $"error case '{c["name"]}' is strict-profile only");
                }
                else
                {
                    // GetValue<bool> throws if 'expected' is absent or not a JSON boolean.
                    _ = c["expected"]!.GetValue<bool>();
                }
            }
        }

        [TestMethod]
        public void GiveEveryParseCaseNamePredicateAndValidFlagWithReasonWhenRejected()
        {
            foreach (var node in LoadVectorRoot()["parse"]!.AsArray())
            {
                var c = node!.AsObject();
                Assert.IsFalse(string.IsNullOrWhiteSpace(c["name"]?.GetValue<string>()), "parse case is missing 'name'");
                Assert.IsFalse(string.IsNullOrWhiteSpace(c["predicate"]?.GetValue<string>()), $"parse case '{c["name"]}' is missing 'predicate'");

                var valid = c["valid"]!.GetValue<bool>();
                if (!valid)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(c["reason"]?.GetValue<string>()), $"rejected parse case '{c["name"]}' must give a 'reason'");
                }
            }
        }

        [TestMethod]
        public void CoverBothParseDirections()
        {
            var parse = LoadVectorRoot()["parse"]!.AsArray();
            Assert.IsTrue(parse.Any(n => n!["valid"]!.GetValue<bool>()), "expected at least one accepted (valid) parse case");
            Assert.IsTrue(parse.Any(n => !n!["valid"]!.GetValue<bool>()), "expected at least one rejected (invalid) parse case");
        }

        [TestMethod]
        public void TagEveryNullTouchingAndNonCoreEvalCaseWithARecognizedProfile()
        {
            foreach (var node in LoadVectorRoot()["eval"]!.AsArray())
            {
                var c = node!.AsObject();
                var profile = c["profile"]?.GetValue<string>();

                // A case whose context binds a ref to JSON null is profile-specific: the UI profile treats
                // null as a participating value (fail-open), the strict profile treats it as a hard error
                // (fail-closed). Either way it is never a core (untagged) case — a core case binds every
                // evaluator, and the two profiles disagree on null.
                var touchesNull = c["values"]!.AsObject().Any(kv => kv.Value is null);
                if (touchesNull)
                {
                    Assert.IsTrue(profile is "ui" or "strict", $"eval case '{c["name"]}' binds a null value and must carry \"profile\": \"ui\" or \"strict\"");
                }

                // "ui" and "strict" are the only profiles in the vocabulary; core cases are left untagged.
                if (profile is not null)
                {
                    Assert.IsTrue(profile is "ui" or "strict", $"eval case '{c["name"]}' has an unrecognized profile '{profile}'");
                }
            }
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
