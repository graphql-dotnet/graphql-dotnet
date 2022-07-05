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

    AddField(new FieldType
    {
      Name = "messageAdded",
      Type = typeof(MessageType),
      StreamResolver = new SourceStreamResolver<Message>(ResolveStream)
    });
  }

  private IObservable<Message> ResolveStream(IResolveFieldContext context)
  {
    return _chat.Messages();
  }
}
```

> See this full schema [here](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL.Tests/Subscription/SubscriptionSchema.cs).
