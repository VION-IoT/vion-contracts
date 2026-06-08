using System;
using System.Reflection;

namespace Vion.Contracts.Events
{
    public static class IMessageExtensions
    {
        public static string GetSchema(this Type type)
        {
            var customAttribute = type.GetCustomAttribute<SchemaAttribute>();
            if (customAttribute == null)
            {
                throw new NullReferenceException($"Missing {nameof(SchemaAttribute)} on '{type.FullName}'");
            }

            return customAttribute.Schema;
        }

        public static string GetSchema(this IMessage msg)
        {
            var attribute = msg.GetType().GetCustomAttribute<SchemaAttribute>();
            if (attribute == null)
            {
                throw new NullReferenceException($"could not get Schema attribute from {msg.GetType().FullName}");
            }

            return attribute.Schema;
        }
    }
}
