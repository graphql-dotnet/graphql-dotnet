using GraphQL.Utilities.Federation;

namespace GraphQL.Federation.SchemaFirst.Sample2;

/// <summary>
/// Retrieves the local instance of <typeparamref name="T"/> from the <see cref="Data"/>
/// class using the specified delegate.
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
public class MyFederatedResolver<T> : IFederatedResolver
    where T : IHasId
{
    private readonly Func<Data, int, Task<T?>> _resolver;
    public MyFederatedResolver(Func<Data, int, Task<T?>> resolver)
    {
        _resolver = resolver;
    }

    public async Task<object?> Resolve(FederatedResolveContext context)
    {
        if (context.Arguments.TryGetValue("id", out object? idValue))
        {
            int id = (int)Convert.ChangeType(idValue, typeof(int))!;
            var data = context.ParentFieldContext.RequestServices!.GetRequiredService<Data>();
            return await _resolver(data, id).ConfigureAwait(false);
        }
        return Task.FromResult<object?>(null);
    }
}
