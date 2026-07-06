// Polyfill for netstandard2.1. RequiresDynamicCodeAttribute was introduced in the .NET 7 BCL; frameworks
// older than net7 (our netstandard2.1 target) don't ship it, so we declare our own copy here to annotate
// reflection-based APIs such as PropertyValueCodec. The trim/AOT analyzers match the attribute by full type
// name, so this self-declared copy flags AOT consumers exactly like the framework-provided one would.
//
// The '!NET7_0_OR_GREATER' guard drops the shim when contracts is compiled for net7 or newer, where the BCL
// already ships the attribute and a duplicate definition would be a compile error. It's a no-op today
// (contracts targets netstandard2.1) and only becomes relevant if contracts is ever multi-targeted onto net7+.

#if !NET7_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
    internal sealed class RequiresDynamicCodeAttribute : Attribute
    {
        public string Message { get; }

        public string? Url { get; set; }

        public RequiresDynamicCodeAttribute(string message)
        {
            Message = message;
        }
    }
}
#endif
