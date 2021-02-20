using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GraphQL.Subscription;
using GraphQL.Types;

namespace GraphQL.Tests.Subscription
{
    public static class SubscriptionSchemaWithReflection
    {
        private const string TypeDefs = @"
            type MessageFrom {
                id: String
                displayName: String
            }

            type Message {
                from: MessageFrom
                content: String
                sentAt: String
            }

            type Subscription {
                messageAdded : Message
                messageAddedByUser(id: String!) : Message
                messageAddedAsync : Message
                messageAddedByUserAsync(id: String!) : Message
                messageGetAll : [Message]
            }
        ";

        public static Chat Chat { get; set; }
        public static ISchema Schema { get; set; }

        public static void Initialize(Chat chat)
        {
            Chat = chat;
            Schema = GraphQL.Types.Schema.For(
                TypeDefs,
                config => config.Types.Include<Subscription>());
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Conventions")]
    public class Subscription
    {
        [GraphQLMetadata(Name = "messageAdded", Type = ResolverType.Subscriber)]
        public IObservable<Message> SubscribeMessageAdded(IResolveEventStreamContext context)
        {
            return SubscriptionSchemaWithReflection.Chat.Messages();
        }

        [GraphQLMetadata(Name = "messageAdded")]
        public Message ResolveMessageAdded(IResolveFieldContext context)
        {
            return context.Source as Message;
        }

        [GraphQLMetadata(Name = "messageGetAll", Type = ResolverType.Subscriber)]
        public IObservable<List<Message>> SubscribeMessageGetAll(IResolveEventStreamContext context)
        {
            return SubscriptionSchemaWithReflection.Chat.MessagesGetAll();
        }

        [GraphQLMetadata(Name = "messageGetAll")]
        public List<Message> ResolveMessageGetAll(IResolveFieldContext context)
        {
            return context.Source as List<Message>;
        }

        [GraphQLMetadata(Name = "messageAddedByUser", Type = ResolverType.Subscriber)]
        public IObservable<Message> SubscribeMessageAddedByUser(IResolveEventStreamContext context, string id)
        {
            var messages = SubscriptionSchemaWithReflection.Chat.Messages();
            return messages.Where(message => message.From.Id == id);
        }

        [GraphQLMetadata(Name = "messageAddedByUser")]
        public Message ResolveMessageAddedByUser(IResolveFieldContext context)
        {
            return context.Source as Message;
        }

        [GraphQLMetadata(Name = "messageAddedAsync", Type = ResolverType.Subscriber)]
        public Task<IObservable<Message>> SubscribeMessageAddedAsync(IResolveEventStreamContext context)
        {
            return SubscriptionSchemaWithReflection.Chat.MessagesAsync();
        }

        [GraphQLMetadata(Name = "messageAddedAsync")]
        public Message ResolveMessageAddedAsync(IResolveFieldContext context)
        {
            return context.Source as Message;
        }

        [GraphQLMetadata(Name = "messageAddedByUserAsync", Type = ResolverType.Subscriber)]
        public async Task<IObservable<Message>> SubscribeMessageAddedByUserAsync(IResolveEventStreamContext context, string id)
        {
            var messages = await SubscriptionSchemaWithReflection.Chat.MessagesAsync();
            return messages.Where(message => message.From.Id == id);
        }

        [GraphQLMetadata(Name = "messageAddedByUserAsync")]
        public Message ResolveMessageAddedByUserAsync(IResolveFieldContext context)
        {
            return context.Source as Message;
        }
    }
}
