using System.Collections;
using GraphQL.DI;

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
