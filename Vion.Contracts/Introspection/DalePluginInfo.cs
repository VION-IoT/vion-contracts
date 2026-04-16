using System.Collections.Generic;

namespace Vion.Contracts.Introspection
{
    /// <summary>
    ///     Describes a particular dale plugin version, including its logic blocks.
    ///     Serialized to JSON by the dale parser and deserialized by cloud-api when a logic block library is uploaded.
    /// </summary>
    public class DalePluginInfo
    {
        /// <summary>
        ///     NuGet package ID, e.g. "MyComppany.LogicBlocks"
        /// </summary>
        public string PackageId { get; set; } = null!;

        /// <summary>
        ///     NuGet package version e.g. "1.0.0"
        /// </summary>
        public string PackageVersion { get; set; } = null!;

        /// <summary>
        ///     Plugin-level annotations (e.g. Branch, Commit, or other metadata).
        /// </summary>
        public Dictionary<string, object> Annotations { get; set; } = [];

        /// <summary>
        ///     All logic blocks of this version of the logic system.
        /// </summary>
        public List<LogicBlockIntrospectionResult> LogicBlocks { get; set; } = [];
    }
}