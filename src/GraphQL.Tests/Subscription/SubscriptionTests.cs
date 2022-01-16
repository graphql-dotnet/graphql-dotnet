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
        protected async Task<SubscriptionExecutionResult> ExecuteSubscribeAsync(ExecutionOptions options)
        {
            var executer = new SubscriptionDocumentExecuter();

            var result = await executer.ExecuteAsync(options);

            result.ShouldBeOfType<SubscriptionExecutionResult>();

            return (SubscriptionExecutionResult)result;
        }

        [Fact]
        public async Task SubscribeGetAll()
        {
            /* Given */
            var addedMessage = new Message
            {
                Content = "test",
                From = new MessageFrom
                {
                    DisplayName = "test",
                    Id = "1"
                },
                SentAt = DateTime.Now.Date
            };

            var chat = new Chat();
            var schema = new ChatSchema(chat);

            /* When */
            var result = await ExecuteSubscribeAsync(new ExecutionOptions
            {
                Query = "subscription messageGetAll { messageGetAll { from { id displayName } content sentAt } }",
                Schema = schema
            });

            chat.AddMessageGetAll(addedMessage);

            /* Then */
            var stream = result.Streams.Values.FirstOrDefault();
            var message = await stream.FirstOrDefaultAsync();

            message.ShouldNotBeNull();
            var data = message.Data.ToDict();
            data.ShouldNotBeNull();
            data["messageGetAll"].ShouldNotBeNull();
        }

        [Fact]
        public async Task SubscribeToContent()
        {
            /* Given */
            var addedMessage = new Message
            {
                Content = "test",
                From = new MessageFrom
                {
                    DisplayName = "test",
                    Id = "1"
                },
                SentAt = DateTime.Now.Date
            };

            var chat = new Chat();
            var schema = new ChatSchema(chat);

            /* When */
            var result = await ExecuteSubscribeAsync(new ExecutionOptions
            {
                Query = "subscription newMessageContent { newMessageContent }",
                Schema = schema
            });

            chat.AddMessage(addedMessage);

            /* Then */
            var stream = result.Streams.Values.FirstOrDefault();
            var message = await stream.FirstOrDefaultAsync();

            message.ShouldNotBeNull();
            var data = message.Data.ToDict();
            data.ShouldNotBeNull();
            data["newMessageContent"].ShouldNotBeNull();
            data["newMessageContent"].ToString().ShouldBe("test");
        }

        [Fact]
        public async Task Subscribe()
        {
            /* Given */
            var addedMessage = new Message
            {
                Content = "test",
                From = new MessageFrom
                {
                    DisplayName = "test",
                    Id = "1"
                },
                SentAt = DateTime.Now.Date
            };
            var chat = new Chat();
            var schema = new ChatSchema(chat);

            /* When */
            var result = await ExecuteSubscribeAsync(new ExecutionOptions
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
        public async Task SubscribeAsync()
        {
            /* Given */
            var addedMessage = new Message
            {
                Content = "test",
                From = new MessageFrom
                {
                    DisplayName = "test",
                    Id = "1"
                },
                SentAt = DateTime.Now.Date
            };
            var chat = new Chat();
            var schema = new ChatSchema(chat);

            /* When */
            var result = await ExecuteSubscribeAsync(new ExecutionOptions
            {
                Query = "subscription MessageAdded { messageAddedAsync { from { id displayName } content sentAt } }",
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
                From = new MessageFrom
                {
                    DisplayName = "test",
                    Id = "1"
                },
                SentAt = DateTime.Now.Date
            };
            var chat = new Chat();
            var schema = new ChatSchema(chat);

            /* When */
            var result = await ExecuteSubscribeAsync(new ExecutionOptions
            {
                Query = "subscription MessageAddedByUser($id:String!) { messageAddedByUser(id: $id) { from { id displayName } content sentAt } }",
                Schema = schema,
                Variables = new Inputs(new Dictionary<string, object>
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
        public async Task SubscribeWithArgumentAsync()
        {
            /* Given */
            var addedMessage = new Message
            {
                Content = "test",
                From = new MessageFrom
                {
                    DisplayName = "test",
                    Id = "1"
                },
                SentAt = DateTime.Now.Date
            };
            var chat = new Chat();
            var schema = new ChatSchema(chat);

            /* When */
            var result = await ExecuteSubscribeAsync(new ExecutionOptions
            {
                Query = "subscription MessageAddedByUser($id:String!) { messageAddedByUserAsync(id: $id) { from { id displayName } content sentAt } }",
                Schema = schema,
                Variables = new Inputs(new Dictionary<string, object>
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

            /* When */
            var result = await ExecuteSubscribeAsync(new ExecutionOptions
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
            error.Path.ShouldBe(new[] { "messageAdded" }, new ROMToStringComparer());
        }
    }
}
