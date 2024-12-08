using System.Globalization;
using GraphQL.Federation.Resolvers;
using GraphQL.Types;

namespace GraphQL.Federation.SchemaFirst.Sample1;

/// <summary>
/// Retrieves the local instance of <typeparamref name="T"/> from the <see cref="Data"/>
/// class using the specified delegate.
/// </summary>
public class MyFederatedResolver<T> : IFederationResolver
    where T : IHasId
{
    private readonly Func<Data, int, Task<T?>> _resolver;

    public MyFederatedResolver(Func<Data, int, Task<T?>> resolver)
    {
        _resolver = resolver;
    }

    public bool MatchKeys(IDictionary<string, object?> representation) => true;

    public object ParseRepresentation(IObjectGraphType graphType, IDictionary<string, object?> representation)
        => (int)Convert.ChangeType(representation["id"], typeof(int), CultureInfo.InvariantCulture)!;

    public async ValueTask<object?> ResolveAsync(IResolveFieldContext context, IObjectGraphType graphType, object parsedRepresentation)
    {
        int id = (int)parsedRepresentation;
        var data = context.RequestServices!.GetRequiredService<Data>();
        return await _resolver(data, id).ConfigureAwait(false);
    }
}
