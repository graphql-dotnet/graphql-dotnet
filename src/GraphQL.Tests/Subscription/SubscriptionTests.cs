using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GraphQL.Subscription;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Subscription
{
    public class SubscriptionTests
    {
        [Fact]
        public async Task Subscribe()
        {
            /* Given */
            var addedMessage = new Message
            {
                Content = "test",
                From = new MessageFrom()
                {
                    DisplayName = "test",
                    Id = "1"
                },
                SentAt = DateTime.Now
            };
            var chat = new Chat();
            var schema = new ChatSchema(chat);
            var sut = new SubscriptionExecuter();

            /* When */
            var result = await sut.SubscribeAsync(new ExecutionOptions
            {
                Query = "subscription MessageAdded { messageAdded { from { id displayName } content sentAt } }",
                Schema = schema
            });

            chat.AddMessage(addedMessage);

            /* Then */
            var stream = result.Streams.Values.FirstOrDefault();
            var message = await stream.FirstOrDefaultAsync();

            message.ShouldNotBeNull();
            message.ShouldBeOfType<ExecutionResult>();
            message.Data.ShouldNotBeNull();
            message.Data.ShouldNotBeAssignableTo<Task>();
        }

        [Fact]
        public async Task SubscribeWithArgument()
        {
            /* Given */
            var addedMessage = new Message
            {
                Content = "test",
                From = new MessageFrom()
                {
                    DisplayName = "test",
                    Id = "1"
                },
                SentAt = DateTime.Now
            };
            var chat = new Chat();
            var schema = new ChatSchema(chat);
            var sut = new SubscriptionExecuter();

            /* When */
            var result = await sut.SubscribeAsync(new ExecutionOptions
            {
                Query = "subscription MessageAddedByUser($id:String!) { messageAddedByUser(id: $id) { from { id displayName } content sentAt } }",
                Schema = schema,
                Inputs = new Inputs(new Dictionary<string, object>()
                {
                    ["id"] = "1"
                })
            });

            chat.AddMessage(addedMessage);

            /* Then */
            var stream = result.Streams.Values.FirstOrDefault();
            var message = await stream.FirstOrDefaultAsync();

            message.ShouldNotBeNull();
            message.ShouldBeOfType<ExecutionResult>();
            message.Data.ShouldNotBeNull();
        }

        [Fact]
        public async Task OnError()
        {
            /* Given */
            var chat = new Chat();
            var schema = new ChatSchema(chat);
            var sut = new SubscriptionExecuter();

            /* When */
            var result = await sut.SubscribeAsync(new ExecutionOptions
            {
                Query = "subscription MessageAdded { messageAdded { from { id displayName } content sentAt } }",
                Schema = schema
            });

            chat.AddError(new Exception("test"));

            /* Then */
            var stream = result.Streams.Values.FirstOrDefault();
            var message = await stream.FirstOrDefaultAsync();

            message.ShouldNotBeNull();
            message.ShouldBeOfType<ExecutionResult>();
            message.Data.ShouldBeNull();
            var error = message.Errors.Single();
            error.InnerException.Message.ShouldBe("test");
            error.Path.ShouldBe(new[] {"messageAdded"});
        }
    }
}
