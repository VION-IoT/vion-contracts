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
        public void GiveEveryEvalCaseNamePredicateValuesAndBooleanExpected()
        {
            foreach (var node in LoadVectorRoot()["eval"]!.AsArray())
            {
                var c = node!.AsObject();
                Assert.IsFalse(string.IsNullOrWhiteSpace(c["name"]?.GetValue<string>()), "eval case is missing 'name'");
                Assert.IsFalse(string.IsNullOrWhiteSpace(c["predicate"]?.GetValue<string>()), $"eval case '{c["name"]}' is missing 'predicate'");
                Assert.IsInstanceOfType<JsonObject>(c["values"], $"eval case '{c["name"]}' is missing a 'values' object");

                // 'expected' is the boolean the predicate yields (the consumer truthiness-tests it into
                // visible/hidden). GetValue<bool> throws if it is absent or not a JSON boolean.
                _ = c["expected"]!.GetValue<bool>();
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
        public void TagNullTouchingEvalCasesWithTheUiProfile()
        {
            foreach (var node in LoadVectorRoot()["eval"]!.AsArray())
            {
                var c = node!.AsObject();
                var profile = c["profile"]?.GetValue<string>();

                // A case whose context binds a ref to JSON null exercises UI-profile-only semantics
                // (RFC 0016's strict profile never faces null by construction) — §2.4 requires the tag.
                var touchesNull = c["values"]!.AsObject().Any(kv => kv.Value is null);
                if (touchesNull)
                {
                    Assert.AreEqual("ui", profile, $"eval case '{c["name"]}' binds a null value and must carry \"profile\": \"ui\"");
                }

                // "ui" is the only profile in the vocabulary; core cases are left untagged.
                if (profile is not null)
                {
                    Assert.AreEqual("ui", profile, $"eval case '{c["name"]}' has an unrecognized profile '{profile}'");
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
