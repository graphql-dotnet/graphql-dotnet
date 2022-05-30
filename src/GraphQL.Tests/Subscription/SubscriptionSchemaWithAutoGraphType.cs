#nullable enable

using System.Reactive.Linq;
using GraphQL.Types;

namespace GraphQL.Tests.Subscription;

public class SubscriptionSchemaWithAutoGraphType : Schema
{
    public SubscriptionSchemaWithAutoGraphType()
    {
        Query = new AutoRegisteringObjectGraphType<QueryType>();
        Mutation = new AutoRegisteringObjectGraphType<MutationType>();
        Subscription = new AutoRegisteringObjectGraphType<SubscriptionType>();

        this.AutoRegister<Message>();
        this.AutoRegister<MessageFrom>();
        this.AutoRegister<ReceivedMessage>();
    }

    public class QueryType
    {
        public static IEnumerable<Message> Messages([FromServices] IChat chat) => chat.AllMessages.Take(100);
    }

    public class MutationType
    {
        public static Message AddMessage([FromServices] IChat chat, ReceivedMessage message) => chat.AddMessage(message);
    }

    public class SubscriptionType
    {
        public static IObservable<Message> MessageAdded([FromServices] IChat chat) => chat.Messages();
        [Name("MessageAddedAsync")]
        public static Task<IObservable<Message>> MessageAddedAsync([FromServices] IChat chat) => chat.MessagesAsync();
        public static IObservable<Message> MessageAddedByUser([FromServices] IChat chat, string id) => chat.Messages().Where(message => message.From.Id == id);
        [Name("MessageAddedByUserAsync")]
        public static async Task<IObservable<Message>> MessageAddedByUserAsync([FromServices] IChat chat, string id) => (await chat.MessagesAsync().ConfigureAwait(false)).Where(message => message.From.Id == id);
        public static IObservable<List<Message>> MessageGetAll([FromServices] IChat chat) => chat.MessagesGetAll();
        public static IObservable<string> NewMessageContent([FromServices] IChat chat) => chat.Messages().Select(message => message.Content);
    }
}
