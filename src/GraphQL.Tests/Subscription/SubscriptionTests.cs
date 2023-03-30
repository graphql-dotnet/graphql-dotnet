using System.Reactive.Linq;
using GraphQL.Instrumentation;

namespace GraphQL.Tests.Subscription;

public class SubscriptionTests
{
    protected async Task<ExecutionResult> ExecuteSubscribeAsync(ExecutionOptions options)
    {
        var executer = new DocumentExecuter();

        var result = await executer.ExecuteAsync(options).ConfigureAwait(false);

        result.Data.ShouldBeNull();

        return result;
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
        }).ConfigureAwait(false);

        chat.AddMessageGetAll(addedMessage);

        /* Then */
        var stream = result.Streams.Values.FirstOrDefault();
        var message = await stream.FirstOrDefaultAsync();

        message.ShouldNotBeNull();
        var data = message.Data.ToDict();
        data.ShouldNotBeNull();
        data["messageGetAll"].ShouldNotBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubscribeToContent(bool useMiddleware)
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
        if (useMiddleware)
        {
            // see
            var my = new NoopMiddleware();
            schema.FieldMiddleware.Use(next => context => my.ResolveAsync(context, next));
        }
        /* When */
        var result = await ExecuteSubscribeAsync(new ExecutionOptions
        {
            Query = "subscription newMessageContent { newMessageContent }",
            Schema = schema
        }).ConfigureAwait(false);

        chat.AddMessage(addedMessage);

        /* Then */
        var stream = result.Streams.Values.FirstOrDefault();
        var message = await stream.FirstOrDefaultAsync();

        message.ShouldNotBeNull();
        var errors = message.Errors;
        errors.ShouldBeNull();
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
        }).ConfigureAwait(false);

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
        }).ConfigureAwait(false);

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
        }).ConfigureAwait(false);

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
        }).ConfigureAwait(false);

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
        }).ConfigureAwait(false);

        chat.AddError(new Exception("test"));

        /* Then */
        var stream = result.Streams.Values.FirstOrDefault();
        var error = await Should.ThrowAsync<ExecutionError>(async () => await stream.FirstOrDefaultAsync()).ConfigureAwait(false);
        error.InnerException.Message.ShouldBe("test");
        error.Path.ShouldBe(new[] { "messageAdded" });
    }

    private class NoopMiddleware : IFieldMiddleware
    {
        public ValueTask<object> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next) => next(context);
    }
}
