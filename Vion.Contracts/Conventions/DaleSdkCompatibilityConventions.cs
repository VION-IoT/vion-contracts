namespace Vion.Contracts.Conventions
{
    /// <summary>
    ///     Annotation keys that let the cloud learn which <c>Vion.Dale.Sdk</c> version a gateway's dale hosts
    ///     and which version each logic-block library needs. All values are <c>MAJOR.MINOR.PATCH</c> strings.
    ///     <para>
    ///         Reader's rule for a library: the effective minimum is <see cref="MinimumDaleSdkVersionAnnotationKey" />,
    ///         else <see cref="TargetDaleSdkVersionAnnotationKey" />. Both are stored as facts, never as a derived
    ///         value; comparison lives in the consumers, not here.
    ///     </para>
    /// </summary>
    public static class DaleSdkCompatibilityConventions
    {
        /// <summary>
        ///     The <c>Vion.Dale.Sdk</c> version dale hosts. Lives in dale's
        ///     <c>ServiceProviderDeclarationPayload.Annotations</c>. Format <c>MAJOR.MINOR.PATCH</c>.
        /// </summary>
        public const string DaleSdkVersionAnnotationKey = "DaleSdkVersion";

        /// <summary>
        ///     The highest <c>Vion.Dale.Sdk</c> reference across a library's dlls. Lives in
        ///     <see cref="Vion.Contracts.Introspection.DalePluginInfo.Annotations" />; always present.
        ///     Format <c>MAJOR.MINOR.PATCH</c>.
        /// </summary>
        public const string TargetDaleSdkVersionAnnotationKey = "TargetDaleSdkVersion";

        /// <summary>
        ///     The minimum <c>Vion.Dale.Sdk</c> version the library's author declared. Lives in
        ///     <see cref="Vion.Contracts.Introspection.DalePluginInfo.Annotations" />; present only when
        ///     declared. Format <c>MAJOR.MINOR.PATCH</c>, validated at pack. The same name is the MSBuild
        ///     property and the <c>AssemblyMetadata</c> key.
        /// </summary>
        public const string MinimumDaleSdkVersionAnnotationKey = "MinimumDaleSdkVersion";
    }
}
