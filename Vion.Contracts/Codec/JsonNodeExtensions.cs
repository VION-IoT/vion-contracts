using System;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Xml;
using Vion.Contracts.TypeRef;

namespace Vion.Contracts.Codec
{
    public static class JsonNodeExtensions
    {
        /// <summary>
        ///     Converts a <see cref="JsonNode" /> value to the boxed CLR primitive corresponding to <paramref name="kind" />.
        /// </summary>
        /// <param name="value">The JSON value, or <c>null</c>.</param>
        /// <param name="kind">The primitive kind to convert to.</param>
        /// <returns>
        ///     <c>bool</c>, <c>long</c> (for any integer kind), <c>double</c> (for any number kind), <c>string</c>,
        ///     <c>DateTime</c>, or <c>TimeSpan</c>; <c>null</c> when <paramref name="value" /> is <c>null</c>.
        /// </returns>
        public static object? ToClrPrimitive(this JsonNode? value, PrimitiveKind kind)
        {
            if (value is null)
            {
                return null;
            }

            return kind switch
            {
                PrimitiveKind.Bool => value.GetValue<bool>(),
                PrimitiveKind.Byte or PrimitiveKind.Short or PrimitiveKind.UShort or PrimitiveKind.Int or PrimitiveKind.UInt or PrimitiveKind.Long => value.GetValue<long>(),
                PrimitiveKind.Float or PrimitiveKind.Double => value.GetValue<double>(),
                PrimitiveKind.String => value.GetValue<string>(),
                PrimitiveKind.DateTime => DateTime.Parse(value.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                PrimitiveKind.Duration => XmlConvert.ToTimeSpan(value.GetValue<string>()),
                _ => throw new NotSupportedException($"Unsupported primitive kind: {kind}"),
            };
        }
    }
}
