namespace Vion.Contracts.Conventions
{
    /// <summary>
    ///     Annotation keys any service provider may set in the loose <c>Annotations</c> dictionary of its
    ///     <see cref="Vion.Contracts.Events.MeshToCloud.ServiceProviderDeclarationPayload" />.
    ///     Keys whose meaning is specific to one SDK live in that SDK's own class
    ///     (see <see cref="DaleSdkCompatibilityConventions" />).
    /// </summary>
    public static class ProviderAnnotationKeys
    {
        /// <summary>
        ///     The provider's own software version, as the component stamps it (SemVer, e.g. <c>0.23.0</c> or
        ///     <c>0.0.0-local</c>). Lives in <c>ServiceProviderDeclarationPayload.Annotations</c>. Shown to
        ///     people, never compared.
        /// </summary>
        public const string VersionAnnotationKey = "Version";
    }
}
