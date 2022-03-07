namespace GraphQL.Resolvers
{
    /// <summary>
    /// Returns an <see cref="IObservable{T}"/> for a field. The <see cref="IObservable{T}"/> provides
    /// a sequence of event notifications (aka 'an event stream') to the subscription execution engine,
    /// which will resolve child fields using the event data as the source. Then the resolved graph is
    /// returned to the client, and the process repeats for further event notifications. 
    /// </summary>
    public interface IEventStreamResolver
    {
        /// <inheritdoc cref="IEventStreamResolver"/>
        ValueTask<IObservable<object?>> SubscribeAsync(IResolveFieldContext context);
    }
}
