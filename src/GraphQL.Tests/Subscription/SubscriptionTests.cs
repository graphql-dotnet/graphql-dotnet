using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
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
                Inputs = new Inputs(new Dictionary<string, object>
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
                Inputs = new Inputs(new Dictionary<string, object>
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
            error.Path.ShouldBe(new[] { "messageAdded" });
        }

        [Fact]
        public void CancellationTokensWorkAsExpected()
        {
            //create a cancellation token source
            var cts = new CancellationTokenSource();
            //grab the token
            var token = cts.Token;
            //signal tokens that they are canceled (performed synchronously, per docs)
            cts.Cancel();
            //dispose of the cancellation token source
            cts.Dispose();
            //at this point the token is still valid
            bool executed = false;
            //attempting to register a callback should immediately run the callback because the token is canceled,
            //pursuant to MS docs. note that the docs also say that this can throw an InvalidOperationException
            //if the source is disposed, but since the token has already been canceled, we should be fine
            token.Register(() => executed = true).Dispose();
            //the callback should run synchronously (per docs), so executed should equal true immediately
            executed.ShouldBeTrue();
        }
    }
}
