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
                config =>
                {
                    config.Types.Include<Subscription>();
                });
        }
    }

    public class Subscription
    {
        [GraphQLMetadata(Name = "messageAdded", Type = ResolverType.Subscriber)]
        public IObservable<Message> SubscribeMessageAdded(ResolveEventStreamContext context)
        {
            return SubscriptionSchemaWithReflection.Chat.Messages();
        }

        [GraphQLMetadata(Name = "messageAdded")]
        public Message ResolveMessageAdded(ResolveFieldContext context)
        {
            return context.Source as Message;
        }

        [GraphQLMetadata(Name = "messageGetAll", Type = ResolverType.Subscriber)]
        public IObservable<List<Message>> SubscribeMessageGetAll(ResolveEventStreamContext context)
        {
            return SubscriptionSchemaWithReflection.Chat.MessagesGetAll();
        }

        [GraphQLMetadata(Name = "messageGetAll")]
        public List<Message> ResolveMessageGetAll(ResolveFieldContext context)
        {
            return context.Source as List<Message>;
        }

        [GraphQLMetadata(Name = "messageAddedByUser", Type = ResolverType.Subscriber)]
        public IObservable<Message> SubscribeMessageAddedByUser(ResolveEventStreamContext context, string id)
        {
            var messages = SubscriptionSchemaWithReflection.Chat.Messages();
            return messages.Where(message => message.From.Id == id);
        }

        [GraphQLMetadata(Name = "messageAddedByUser")]
        public Message ResolveMessageAddedByUser(ResolveFieldContext context)
        {
            return context.Source as Message;
        }

        [GraphQLMetadata(Name = "messageAddedAsync", Type = ResolverType.Subscriber)]
        public Task<IObservable<Message>> SubscribeMessageAddedAsync(ResolveEventStreamContext context)
        {
            return SubscriptionSchemaWithReflection.Chat.MessagesAsync();
        }

        [GraphQLMetadata(Name = "messageAddedAsync")]
        public Message ResolveMessageAddedAsync(ResolveFieldContext context)
        {
            return context.Source as Message;
        }

        [GraphQLMetadata(Name = "messageAddedByUserAsync", Type = ResolverType.Subscriber)]
        public async Task<IObservable<Message>> SubscribeMessageAddedByUserAsync(ResolveEventStreamContext context, string id)
        {
            var messages = await SubscriptionSchemaWithReflection.Chat.MessagesAsync();
            return messages.Where(message => message.From.Id == id);
        }

        [GraphQLMetadata(Name = "messageAddedByUserAsync")]
        public Message ResolveMessageAddedByUserAsync(ResolveFieldContext context)
        {
            return context.Source as Message;
        }
    }
}
