namespace GraphQL.Resolvers
{
    public interface IEventStreamResolver
    {
        ValueTask<IObservable<object?>> SubscribeAsync(IResolveFieldContext context);
    }
}
