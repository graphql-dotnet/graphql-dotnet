#nullable enable

using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using GraphQL.Resolvers;
using GraphQL.Subscription;
using GraphQL.Types;

namespace GraphQL.Tests.Subscription
{
    public class SubscriptionSchemaWithAutoGraphType : Schema
    {
        public SubscriptionSchemaWithAutoGraphType(IChat chat)
        {
            Query = new AutoRegisteringObjectGraphType<QueryType>();
            Mutation = new ChatMutation(chat);
            Subscription = new ChatSubscriptions(chat);
        }

        public class QueryType
        {
            public static IObservable<Message> MessageAdded([FromServices] IChat chat) => chat.Messages();
            public static Task<IObservable<Message>> MessageAddedAsync([FromServices] IChat chat) => chat.MessagesAsync();
            public static IObservable<Message> MessageAddedByUser([FromServices] IChat chat, string id) => chat.Messages().Where(message => message.From.Id == id);
            public static async Task<IObservable<Message>> MessageAddedByUserAsync([FromServices] IChat chat, string id) => (await chat.MessagesAsync()).Where(message => message.From.Id == id);
            public static IObservable<List<Message>> MessageGetAll([FromServices] IChat chat) => chat.MessagesGetAll();
            public static IObservable<string> NewMessageContent([FromServices] IChat chat) => chat.Messages().Select(message => message.Content);
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
}
