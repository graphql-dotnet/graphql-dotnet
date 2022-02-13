using System.Collections;
using GraphQL.DI;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.DI;

internal sealed class DependencyInjectionAdapterData : IEnumerable<object[]>
{
    public static IEnumerable<IDependencyInjectionAdapter> GetDependencyInjectionAdapters(Action<IServiceRegister> configure)
    {
        yield return new MicrosoftAdapter(configure);
        //yield return new StructureMapAdapter(configure);
        //yield return new CastleWindsorAdapter(configure);
    }

    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { "MSDI" };
        //yield return new object[] { "StructureMap" };
        //yield return new object[] { "CastleWindsor" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal interface IDependencyInjectionAdapter
{
    public IServiceProvider ServiceProvider { get; }
}

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
