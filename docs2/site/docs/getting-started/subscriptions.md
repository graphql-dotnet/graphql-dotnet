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

## Resource Cleanup and Disposal

When a client disconnects or unsubscribes from a subscription, the server will call `Dispose()` on
the `IDisposable` returned by `IObservable<T>.Subscribe()`. This allows your service to implement
cleanup logic such as releasing database connections, canceling background tasks, or freeing other resources.

### Custom IObservable Implementation

Here's an example of a custom `IObservable<T>` implementation that properly handles disposal:

```csharp
public class MessageObservable : IObservable<Message>
{
    private readonly List<IObserver<Message>> _observers = new();
    private readonly object _lock = new();

    public void Publish(Message message)
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

    public IDisposable Subscribe(IObserver<Message> observer)
    {
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));

        lock (_lock)
        {
            _observers.Add(observer);
        }

        // Return a disposable that will be called when the client disconnects
        return new Unsubscriber(this, observer);
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
            // This is called when the client disconnects or unsubscribes
            var source = Interlocked.Exchange(ref _source, null);
            if (source == null)
                return;

            var observer = Interlocked.Exchange(ref _observer, null);
            lock (source._lock)
            {
                source._observers.Remove(observer!);
            }

            // Perform any additional cleanup here:
            // - Close database connections
            // - Cancel background tasks
            // - Release external resources
            // - Notify other services of disconnection
        }
    }
}
```

### Key Points

- The `IDisposable` returned by `Subscribe()` is called when the subscription ends
- Disposal can occur due to client disconnect, explicit unsubscribe, or server shutdown
- Use `Interlocked.Exchange` for thread-safe cleanup in concurrent scenarios
- The disposal pattern integrates with the GraphQL Server transport layer (WebSockets, etc.)

### Using System.Reactive

If you prefer, you can use the [System.Reactive](https://www.nuget.org/packages/System.Reactive) library
which provides rich operators for creating and manipulating observables:

```csharp
using System.Reactive.Linq;
using System.Reactive.Subjects;

public class ChatService : IChat
{
    private readonly Subject<Message> _messageStream = new();

    public IObservable<Message> Messages()
    {
        // Subject<T> implements IObservable<T> and handles disposal automatically
        return _messageStream.AsObservable();
    }

    public void AddMessage(Message message)
    {
        _messageStream.OnNext(message);
    }
}
```
