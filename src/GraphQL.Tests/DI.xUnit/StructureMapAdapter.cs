using GraphQL.DI;
using GraphQL.StructureMap;
using Microsoft.Extensions.DependencyInjection;
using StructureMap;

namespace GraphQL.Tests.DI;

internal sealed class StructureMapAdapter : IDependencyInjectionAdapter
{
    private readonly GraphQLBuilder _builder;
    private IServiceProvider _provider;

    public StructureMapAdapter(Action<IServiceRegister> configure)
    {
        _builder = new GraphQLBuilder(new Registry(), b => configure(b.Services));
    }

    public IServiceProvider ServiceProvider => _provider ??= ((Registry)_builder.Registry).BuildServiceProvider();
}
