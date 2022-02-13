namespace GraphQL.Tests.DI;

internal sealed class DependencyInjectionDataAttribute : ClassDataAttribute
{
    public DependencyInjectionDataAttribute() : base(typeof(DependencyInjectionAdapterData))
    {
    }
}
