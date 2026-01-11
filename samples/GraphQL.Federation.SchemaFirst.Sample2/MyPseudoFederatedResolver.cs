using System.Globalization;
using GraphQL.Federation.Resolvers;
using GraphQL.Federation.SchemaFirst.Sample2.Schema;
using GraphQL.Types;

namespace GraphQL.Federation.SchemaFirst.Sample2;

/// <summary>
/// Creates a new instance of <typeparamref name="T"/> for any object requested to be resolved.
/// Used for <see cref="Category"/> since there is no data stored in this repository for categories.
/// </summary>
public class MyPseudoFederatedResolver<T> : IFederationResolver
    where T : IHasId, new()
{
    public bool MatchKeys(IDictionary<string, object?> representation) => true;

    public object ParseRepresentation(IComplexGraphType graphType, IDictionary<string, object?> representation, IValueConverter valueConverter)
        => (int)Convert.ChangeType(representation["id"], typeof(int), CultureInfo.InvariantCulture)!;

    public ValueTask<object?> ResolveAsync(IResolveFieldContext context, IComplexGraphType graphType, object parsedRepresentation)
    {
        int id = (int)parsedRepresentation;
        return ValueTask.FromResult<object?>(new T() { Id = id });
    }
}
