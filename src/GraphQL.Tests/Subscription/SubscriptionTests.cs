using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GraphQL.Subscription;
using Newtonsoft.Json.Linq;
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
            Assert.NotNull(message);
            Assert.IsType<ExecutionResult>(message);
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
            Assert.NotNull(message);
            Assert.IsType<ExecutionResult>(message);
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
            Assert.NotNull(message);
            Assert.IsType<ExecutionResult>(message);
            Assert.Null(message.Data);
            Assert.Contains("test", message.Errors.Select(e => e.InnerException.Message));
        }
    }
}
