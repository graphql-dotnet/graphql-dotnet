using StructureMap;

namespace GraphQL.StructureMap;

// TODO: move in tests?
public static class RegistryExtensions
{
    public static IServiceProvider BuildServiceProvider(this Registry registry)
    {
        return new ContainerAdapter(new Container(registry));
    }

    private sealed class ContainerAdapter : IServiceProvider
    {
        public ContainerAdapter(IContainer container)
        {
            Container = container;
        }

        public IContainer Container { get; }

        public object GetService(Type serviceType) => Container.GetInstance(serviceType);
    }
}

