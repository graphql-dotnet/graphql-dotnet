## Subscriptions

Subscriptions are supported through the use of `IObservable<T>`.  You will need a server that supports a Subscription protocol.  The [GraphQL Server](https://github.com/graphql-dotnet/server/) project provides a .NET Core server that implements the Apollo GraphQL subscription protocol.  See the [GraphQL Server project samples](https://github.com/graphql-dotnet/server/tree/develop/samples).

```csharp
public class ChatSubscriptions : ObjectGraphType
{
  private readonly IChat _chat;

  public ChatSubscriptions(IChat chat)
  {
    _chat = chat;

    AddField(new EventStreamFieldType
    {
      Name = "messageAdded",
      Type = typeof(MessageType),
      Resolver = new FuncFieldResolver<Message>(ResolveMessage),
      Subscriber = new EventStreamResolver<Message>(Subscribe)
    });
  }

  private Message ResolveMessage(ResolveFieldContext context)
  {
    return context.Source as Message;
  }

  private IObservable<Message> Subscribe(ResolveEventStreamContext context)
  {
    return _chat.Messages();
  }
}
```

See this full schema [here](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL.Tests/Subscription/SubscriptionSchema.cs).
