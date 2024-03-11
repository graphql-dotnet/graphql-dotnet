#if NETCOREAPP1_0_OR_GREATER
using System.Collections.Immutable;

namespace GraphQL.Conversion;

internal sealed class ImmutableArrayListConverterFactory : ListConverterFactoryBase
{
    public override Func<object?[], object> GetConversion<T>()
        => list => ImmutableArray.CreateRange(list.Cast<T>());
}

internal sealed class ImmutableListConverterFactory : ListConverterFactoryBase
{
    public override Func<object?[], object> GetConversion<T>()
        => list => ImmutableList.CreateRange(list.Cast<T>());
}

internal sealed class ImmutableSetListConverterFactory : ListConverterFactoryBase
{
    public override Func<object?[], object> GetConversion<T>()
        => list => ImmutableHashSet.CreateRange(list.Cast<T>());
}
#endif
