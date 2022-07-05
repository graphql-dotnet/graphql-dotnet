using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Tests.Subscription;

public class ChatSchema : Schema
{
    public ChatSchema(IChat chat)
    {
        Query = new ChatQuery(chat);
        Mutation = new ChatMutation(chat);
        Subscription = new ChatSubscriptions(chat);
    }
}

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
            StreamResolver = new SourceStreamResolver<Message>(Subscribe)
        });

        AddField(new FieldType
        {
            Name = "messageAddedByUser",
            Arguments = new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
            ),
            Type = typeof(MessageType),
            StreamResolver = new SourceStreamResolver<Message>(SubscribeById)
        });

        AddField(new FieldType
        {
            Name = "messageAddedAsync",
            Type = typeof(MessageType),
            StreamResolver = new SourceStreamResolver<Message>(SubscribeAsync)
        });

        AddField(new FieldType
        {
            Name = "messageAddedByUserAsync",
            Arguments = new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
            ),
            Type = typeof(MessageType),
            StreamResolver = new SourceStreamResolver<Message>(SubscribeByIdAsync)
        });

        AddField(new FieldType
        {
            Name = "messageGetAll",
            Type = typeof(ListGraphType<MessageType>),
            StreamResolver = new SourceStreamResolver<List<Message>>(_ => _chat.MessagesGetAll())
        });

        AddField(new FieldType
        {
            Name = "newMessageContent",
            Type = typeof(StringGraphType),
            StreamResolver = new SourceStreamResolver<string>(context => Subscribe(context).Select(message => message.Content))
        });
    }

    private IObservable<Message> SubscribeById(IResolveFieldContext context)
    {
        var id = context.GetArgument<string>("id");

        var messages = _chat.Messages();

        return messages.Where(message => message.From.Id == id);
    }

    private async ValueTask<IObservable<Message>> SubscribeByIdAsync(IResolveFieldContext context)
    {
        var id = context.GetArgument<string>("id");

        var messages = await _chat.MessagesAsync().ConfigureAwait(false);
        return messages.Where(message => message.From.Id == id);
    }

    private IObservable<Message> Subscribe(IResolveFieldContext context)
    {
        return _chat.Messages();
    }

    private async ValueTask<IObservable<Message>> SubscribeAsync(IResolveFieldContext context)
    {
        return await _chat.MessagesAsync().ConfigureAwait(false);
    }
}

public class ChatMutation : ObjectGraphType<object>
{
    public ChatMutation(IChat chat)
    {
        Field<MessageType>("addMessage",
            arguments: new QueryArguments(
                new QueryArgument<MessageInputType> { Name = "message" }
            ),
            resolve: context =>
            {
                var receivedMessage = context.GetArgument<ReceivedMessage>("message");
                var message = chat.AddMessage(receivedMessage);
                return message;
            });
    }
}

public class ChatQuery : ObjectGraphType
{
    public ChatQuery(IChat chat)
    {
        Field<ListGraphType<MessageType>>("messages", resolve: context => chat.AllMessages.Take(100));
    }
}

public class MessageType : ObjectGraphType<Message>
{
    public MessageType()
    {
        Field(o => o.Content);
        Field(o => o.SentAt);
        Field(o => o.From, false, typeof(MessageFromType)).Resolve(ResolveFrom);
    }

    private MessageFrom ResolveFrom(IResolveFieldContext<Message> context)
    {
        var message = context.Source;
        return message.From;
    }
}

public class MessageInputType : InputObjectGraphType
{
    public MessageInputType()
    {
        Field<StringGraphType>("fromId");
        Field<StringGraphType>("content");
        Field<DateGraphType>("sentAt");
    }
}

public class MessageFromType : ObjectGraphType<MessageFrom>
{
    public MessageFromType()
    {
        Field(o => o.Id);
        Field(o => o.DisplayName);
    }
}

public class Message
{
    public MessageFrom From { get; set; }

    public string Content { get; set; }

    public DateTime SentAt { get; set; }
}

public class MessageFrom
{
    public string Id { get; set; }

    public string DisplayName { get; set; }
}

public class ReceivedMessage
{
    public string FromId { get; set; }

    public string Content { get; set; }

    public DateTime SentAt { get; set; }
}

public interface IChat
{
    ConcurrentStack<Message> AllMessages { get; }

    Message AddMessage(Message message);

    IObservable<Message> Messages();
    IObservable<List<Message>> MessagesGetAll();

    Message AddMessage(ReceivedMessage message);

    Task<IObservable<Message>> MessagesAsync();
}

public class Chat : IChat
{
    private readonly ISubject<Message> _messageStream = new ReplaySubject<Message>(1);
    private readonly ISubject<List<Message>> _allMessageStream = new ReplaySubject<List<Message>>(1);

    public Chat()
    {
        AllMessages = new ConcurrentStack<Message>();
        Users = new ConcurrentDictionary<string, string>
        {
            ["1"] = "developer",
            ["2"] = "tester"
        };
    }

    public ConcurrentDictionary<string, string> Users { get; set; }

    public ConcurrentStack<Message> AllMessages { get; }

    public Message AddMessage(ReceivedMessage message)
    {
        if (!Users.TryGetValue(message.FromId, out var displayName))
        {
            displayName = "(unknown)";
        }

        return AddMessage(new Message
        {
            Content = message.Content,
            SentAt = message.SentAt,
            From = new MessageFrom
            {
                DisplayName = displayName,
                Id = message.FromId
            }
        });
    }

    public async Task<IObservable<Message>> MessagesAsync()
    {
        //pretend we are doing something async here
        await Task.Delay(100).ConfigureAwait(false);
        return Messages();
    }

    public List<Message> AddMessageGetAll(Message message)
    {
        AllMessages.Push(message);
        var l = new List<Message>(AllMessages);
        _allMessageStream.OnNext(l);
        return l;
    }

    public Message AddMessage(Message message)
    {
        AllMessages.Push(message);
        _messageStream.OnNext(message);
        return message;
    }

    public IObservable<Message> Messages()
    {
        return _messageStream.AsObservable();
    }

    public IObservable<List<Message>> MessagesGetAll()
    {
        return _allMessageStream.AsObservable();
    }

    public void AddError(Exception exception)
    {
        _messageStream.OnError(exception);
    }
}
