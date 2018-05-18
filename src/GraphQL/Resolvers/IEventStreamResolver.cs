using System;
using System.Threading.Tasks;
using GraphQL.Subscription;

namespace GraphQL.Resolvers
{
    public interface IEventStreamResolver
    {
        IObservable<object> Subscribe(ResolveEventStreamContext context);
    }

    public interface IAsyncEventStreamResolver
    {
        Task<IObservable<object>> SubscribeAsync(ResolveEventStreamContext context);
    }
}
