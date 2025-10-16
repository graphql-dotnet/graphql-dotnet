using System.Reactive.Linq;
using GraphQL.Instrumentation;

namespace GraphQL.Tests.Subscription;

public class SubscriptionTests
{
    private readonly DateTimeOffset DateConst = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

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
            SentAt = DateConst
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
        var stream = result.Streams!.Values.First();
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
            SentAt = DateConst
        };

        var chat = new Chat();
        var schema = new ChatSchema(chat);
        if (useMiddleware)
        {
            // see https://github.com/graphql-dotnet/graphql-dotnet/pull/3568
            var my = new NoopMiddleware();
            schema.FieldMiddleware.Use(next => context => my.ResolveAsync(context, next));
        }
        /* When */
        var result = await ExecuteSubscribeAsync(new ExecutionOptions
        {
            Query = "subscription newMessageContent { newMessageContent }",
            Schema = schema
        });

        chat.AddMessage(addedMessage);

        /* Then */
        var stream = result.Streams!.Values.First();
        var message = await stream.FirstOrDefaultAsync();

        message.ShouldNotBeNull();
        var errors = message.Errors;
        errors.ShouldBeNull();
        var data = message.Data.ToDict();
        data.ShouldNotBeNull();
        data["newMessageContent"].ShouldNotBeNull();
        data["newMessageContent"]!.ToString().ShouldBe("test");
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
            SentAt = DateConst
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
        var stream = result.Streams.ShouldNotBeNull().Values.First();
        var message = await stream.FirstOrDefaultAsync();

        message.ShouldBeSimilarTo("""
            {"data":{"messageAdded":{"from":{"id":"1","displayName":"test"},"content":"test","sentAt":"2024-01-01T00:00:00\u002B00:00"}}}
            """);
    }

    [Fact]
    public async Task SubscribeInt()
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
            SentAt = DateConst
        };
        var chat = new Chat();
        var schema = new ChatSchema(chat);

        /* When */
        var result = await ExecuteSubscribeAsync(new ExecutionOptions
        {
            Query = "subscription { messageCounter }",
            Schema = schema
        });

        chat.AddMessage(addedMessage);

        /* Then */
        var stream = result.Streams.ShouldNotBeNull().Values.First();
        var message = await stream.FirstOrDefaultAsync();

        message.ShouldBeSimilarTo("""
            {"data":{"messageCounter":1}}
            """);
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
            SentAt = DateConst
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
        var stream = result.Streams.ShouldNotBeNull().Values.First();
        var message = await stream.FirstOrDefaultAsync();

        message.ShouldBeSimilarTo("""
            {"data":{"messageAddedAsync":{"from":{"id":"1","displayName":"test"},"content":"test","sentAt":"2024-01-01T00:00:00\u002B00:00"}}}
            """);
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
            SentAt = DateConst
        };
        var chat = new Chat();
        var schema = new ChatSchema(chat);

        /* When */
        var result = await ExecuteSubscribeAsync(new ExecutionOptions
        {
            Query = "subscription MessageAddedByUser($id:String!) { messageAddedByUser(id: $id) { from { id displayName } content sentAt } }",
            Schema = schema,
            Variables = new Inputs(new Dictionary<string, object?>
            {
                ["id"] = "1"
            })
        });

        chat.AddMessage(addedMessage);

        /* Then */
        var stream = result.Streams.ShouldNotBeNull().Values.First();
        var message = await stream.FirstOrDefaultAsync();

        message.ShouldBeSimilarTo("""
            {"data":{"messageAddedByUser":{"from":{"id":"1","displayName":"test"},"content":"test","sentAt":"2024-01-01T00:00:00\u002B00:00"}}}
            """);
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
            SentAt = DateConst
        };
        var chat = new Chat();
        var schema = new ChatSchema(chat);

        /* When */
        var result = await ExecuteSubscribeAsync(new ExecutionOptions
        {
            Query = "subscription MessageAddedByUser($id:String!) { messageAddedByUserAsync(id: $id) { from { id displayName } content sentAt } }",
            Schema = schema,
            Variables = new Inputs(new Dictionary<string, object?>
            {
                ["id"] = "1"
            })
        });

        chat.AddMessage(addedMessage);

        /* Then */
        var stream = result.Streams.ShouldNotBeNull().Values.First();
        var message = await stream.FirstOrDefaultAsync();

        message.ShouldBeSimilarTo("""
            {"data":{"messageAddedByUserAsync":{"from":{"id":"1","displayName":"test"},"content":"test","sentAt":"2024-01-01T00:00:00\u002B00:00"}}}
            """);
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
        var stream = result.Streams.ShouldNotBeNull().Values.First();
        var error = await Should.ThrowAsync<ExecutionError>(async () => await stream.FirstOrDefaultAsync());
        error.InnerException!.Message.ShouldBe("test");
        error.Path.ShouldBe(["messageAdded"]);
    }

    private class NoopMiddleware : IFieldMiddleware
    {
        public ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next) => next(context);
    }
}
