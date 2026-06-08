using System.Text.Json;
using Vion.Contracts.Introspection;

namespace Vion.Contracts.Test.Introspection
{
    [TestClass]
    public class LogicBlockIntrospectionResultShould
    {
        // Mirrors the platform wire convention: camelCase property names + dictionary keys.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void RejectAServicePropertyWithoutASchema()
        {
            // Schema is required on the dale->cloud introspection contract: a property carrying no schema
            // is malformed and rejected at deserialisation. Greenfield — introspection history is rebuilt,
            // so there are no pre-rich-types null-schema definitions to tolerate.
            const string noSchema = "{\"services\":[{\"identifier\":\"s\",\"properties\":[{\"identifier\":\"MaxCurrent\"}]}]}";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<LogicBlockIntrospectionResult>(noSchema, WireOptions));
        }

        [TestMethod]
        public void RejectAServiceMeasuringPointWithoutASchema()
        {
            const string noSchema = "{\"services\":[{\"identifier\":\"s\",\"measuringPoints\":[{\"identifier\":\"Power\"}]}]}";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<LogicBlockIntrospectionResult>(noSchema, WireOptions));
        }
    }
}
