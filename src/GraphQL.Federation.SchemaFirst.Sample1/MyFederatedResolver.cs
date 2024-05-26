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

    public Type SourceType => typeof(IDictionary<string, object?>);

    public async ValueTask<object?> ResolveAsync(IResolveFieldContext context, object source)
    {
        var arguments = (IDictionary<string, object?>)source;
        if (arguments.TryGetValue("id", out object? idValue))
        {
            int id = (int)Convert.ChangeType(idValue, typeof(int))!;
            var data = context.RequestServices!.GetRequiredService<Data>();
            return await _resolver(data, id).ConfigureAwait(false);
        }
        return Task.FromResult<object?>(null);
    }
}
