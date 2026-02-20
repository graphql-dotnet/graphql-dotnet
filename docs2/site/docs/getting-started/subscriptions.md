# Subscriptions

Subscriptions are supported through the use of `IObservable<T>`. You will need a server that
supports a Subscription protocol.  The [GraphQL Server](https://github.com/graphql-dotnet/server/)
project provides a .NET Core server that implements the Apollo GraphQL subscription protocol.
See the [GraphQL Server project samples](https://github.com/graphql-dotnet/server/tree/develop/samples).

Instead of using the `query` or `mutation` keyword you are required to use `subscription`.
Similar to a `query` and `mutation`, you can omit the `Operation` name if there is only a
single operation in the request.

```graphql
subscription MessageAdded {
  messageAdded {
    from {
      id
      displayName
    }
    content
    sentAt
  }
}
```

```csharp
public class ChatSubscriptions : ObjectGraphType
{
  private readonly IChat _chat;

  public ChatSubscriptions(IChat chat)
  {
    _chat = chat;

    Field<MessageType, Message>("messageAdded")
      .ResolveStream(ResolveStream);
  }

  private IObservable<Message> ResolveStream(IResolveFieldContext context)
  {
    return _chat.Messages();
  }
}
```

> See this full schema [here](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL.Tests/Subscription/SubscriptionSchema.cs).

## Subscription Lifecycle and Cleanup

When a client closes a subscription, the server will attempt to dispose of the `IObservable<T>` 
returned from your `ResolveStream` or `ResolveStreamAsync` method by calling `Dispose()` on 
the subscription object (the `IDisposable` returned from `IObservable<T>.Subscribe()`). This 
allows you to implement cleanup code and release any resources associated with the subscription.

### Custom IObservable Implementation with Cleanup

Here's an example of a custom `IObservable<T>` implementation that properly handles disposal:

```csharp
public class MessageObservable : IObservable<Message>
{
    private readonly List<IObserver<Message>> _observers = new();
    private readonly object _lock = new();

    public IDisposable Subscribe(IObserver<Message> observer)
    {
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));
        
        lock (_lock)
        {
            _observers.Add(observer);
        }
        
        return new Unsubscriber(this, observer);
    }

    public void SendMessage(Message message)
    {
        IObserver<Message>[] observers;
        lock (_lock)
        {
            observers = _observers.ToArray();
        }
        
        foreach (var observer in observers)
        {
            observer.OnNext(message);
        }
    }

    private class Unsubscriber : IDisposable
    {
        private MessageObservable? _source;
        private IObserver<Message>? _observer;

        public Unsubscriber(MessageObservable source, IObserver<Message> observer)
        {
            _source = source;
            _observer = observer;
        }

        public void Dispose()
        {
            var source = Interlocked.Exchange(ref _source, null);
            if (source == null)
                return;
                
            // observer is non-null here because both fields are initialized in the
            // constructor and only set to null together during the first Dispose() call
            var observer = Interlocked.Exchange(ref _observer, null);
            
            lock (source._lock)
            {
                source._observers.Remove(observer!);
            }
            
            // Perform any additional cleanup here
            // For example: close database connections, stop timers, etc.
        }
    }
}
```

This pattern ensures that when a subscription is closed:
- The observer is removed from the list of active observers
- Resources can be properly cleaned up in the `Dispose()` method
- Multiple calls to `Dispose()` are handled safely

You can use popular reactive libraries like [System.Reactive](https://github.com/dotnet/reactive) 
which provide robust `IObservable<T>` implementations with built-in disposal handling, such as 
`Subject<T>`, `ReplaySubject<T>`, and `BehaviorSubject<T>`.
