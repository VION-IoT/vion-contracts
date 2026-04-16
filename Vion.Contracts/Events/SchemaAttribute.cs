using System;

namespace Vion.Contracts.Events
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class SchemaAttribute : Attribute
    {
        public string Schema { get; init; }

        public SchemaAttribute(string schema)
        {
            Schema = schema;
        }
    }
}