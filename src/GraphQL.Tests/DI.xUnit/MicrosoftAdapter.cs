using GraphQL.DI;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.DI;

internal sealed class MicrosoftAdapter : IDependencyInjectionAdapter
{
    private readonly MicrosoftDI.GraphQLBuilder _builder;
    private IServiceProvider _provider;

    public MicrosoftAdapter(Action<IServiceRegister> configure)
    {
        _builder = new MicrosoftDI.GraphQLBuilder(new ServiceCollection(), b => configure(b.Services));
    }

    public IServiceProvider ServiceProvider => _provider ??= _builder.ServiceCollection.BuildServiceProvider();
}
