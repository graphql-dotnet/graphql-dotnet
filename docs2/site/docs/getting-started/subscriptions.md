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

## Cleanup when a subscription stops

A subscription resolver returns an `IObservable<T>`, and the transport layer subscribes to it.
When the client unsubscribes (or disconnects), the transport layer disposes the `IDisposable`
returned by `Subscribe(...)`.

If your subscription stream allocates resources (timers, event handlers, sockets, etc.), put
cleanup logic in that disposable.

```csharp
public sealed class MessageStream : IObservable<string>
{
    public IDisposable Subscribe(IObserver<string> observer)
    {
        var timer = new Timer(_ => observer.OnNext(DateTime.UtcNow.ToString("O")),
            state: null,
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromSeconds(1));

        return new TimerSubscription(timer);
    }

    private sealed class TimerSubscription : IDisposable
    {
        private readonly Timer _timer;

        public TimerSubscription(Timer timer)
        {
            _timer = timer;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
```

In a GraphQL subscription, this means your resource cleanup runs automatically when the
subscription is completed by the client.
