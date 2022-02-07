namespace ImportService
{
    using LaunchSharp.DependencyContainer.SimpleInjector.Packaging;
    using SimpleInjector;

    /// <summary>
    /// Controls which concrete classes are injected for interfaces.
    /// </summary>
    internal class DependencyPackage : IPackage
    {
        /// <summary>
        /// Define which concrete classes are injected for interfaces.
        /// </summary>
        /// <param name="container">The dependency injection container.</param>
        public void RegisterServices(Container container)
        {
            container.Register<Application>();
        }
    }
}
