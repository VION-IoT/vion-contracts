using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Vion.Contracts.Codec
{
    /// <summary>
    ///     Caches <c>Enum.GetName</c> and <c>Enum.Parse</c> lookups per CLR enum <see cref="Type" />
    ///     to avoid the per-call reflection cost of <see cref="Enum.GetName(Type, object)" /> on the
    ///     codec hot path. Thread-safe (<see cref="ConcurrentDictionary{TKey,TValue}" />).
    /// </summary>
    [RequiresDynamicCode("Enum.GetValues over a runtime Type requires dynamic code; not supported under NativeAOT.")]
    internal static class EnumNameCache
    {
        private static readonly ConcurrentDictionary<Type, EnumMap> Cache = new();

        /// <summary>
        ///     Returns the declared name for <paramref name="value" /> on its enum type. Throws
        ///     <see cref="PropertyValueDecodeException" /> if the value is not a defined enum member.
        /// </summary>
        public static string GetName(Type enumType, object value)
        {
            return Cache.GetOrAdd(enumType, t => new EnumMap(t)).GetName(value);
        }

        /// <summary>
        ///     Parses a member <paramref name="name" /> back to its boxed enum value. Throws
        ///     <see cref="PropertyValueDecodeException" /> for unknown names.
        /// </summary>
        public static object Parse(Type enumType, string name)
        {
            return Cache.GetOrAdd(enumType, t => new EnumMap(t)).Parse(name);
        }

        private sealed class EnumMap
        {
            private readonly Dictionary<string, object> _fromName;

            private readonly Dictionary<object, string> _toName;

            public EnumMap(Type t)
            {
                var values = Enum.GetValues(t);
                _toName = new Dictionary<object, string>(values.Length);
                _fromName = new Dictionary<string, object>(values.Length);
                foreach (var v in values)
                {
                    var name = Enum.GetName(t, v) ?? v.ToString()!;
                    _toName[v] = name;
                    _fromName[name] = v;
                }
            }

            public string GetName(object value)
            {
                return _toName.TryGetValue(value, out var name) ? name :
                           throw new PropertyValueDecodeException($"Enum value '{value}' is not a defined member of '{value.GetType().FullName}'.");
            }

            public object Parse(string name)
            {
                return _fromName.TryGetValue(name, out var v) ? v : throw new PropertyValueDecodeException($"Unknown enum member: '{name}'.");
            }
        }
    }
}
