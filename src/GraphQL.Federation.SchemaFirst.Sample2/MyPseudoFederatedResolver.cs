using GraphQL.Federation.SchemaFirst.Sample2.Schema;
using GraphQL.Utilities.Federation;

namespace GraphQL.Federation.SchemaFirst.Sample2;

/// <summary>
/// Creates a new instance of <typeparamref name="T"/> for any object requested to be resolved.
/// Used for <see cref="Category"/> since there is no data stored in this repository for categories.
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
public class MyPseudoFederatedResolver<T> : IFederatedResolver
    where T : IHasId, new()
{
    public Task<object?> Resolve(FederatedResolveContext context)
    {
        if (context.Arguments.TryGetValue("id", out object? idValue))
        {
            int id = (int)Convert.ChangeType(idValue, typeof(int))!;
            return Task.FromResult<object?>(new T() { Id = id! });
        }
        return Task.FromResult<object?>(null);
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
