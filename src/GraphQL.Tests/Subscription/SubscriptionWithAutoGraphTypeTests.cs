using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Subscription;

public class SubscriptionWithAutoGraphTypeTests
{
    private readonly Chat Chat = new();

    protected async Task<ExecutionResult> ExecuteSubscribeAsync(ExecutionOptions options)
    {
        var executer = new DocumentExecuter();
        var services = new ServiceCollection();
        services.AddSingleton<IChat>(Chat);
        var provider = services.BuildServiceProvider();

        options.Schema = new SubscriptionSchemaWithAutoGraphType();
        options.RequestServices = provider;

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

        /* When */
        var result = await ExecuteSubscribeAsync(new ExecutionOptions
        {
            Query = "subscription messageGetAll { messageGetAll { from { id displayName } content sentAt } }",
        }).ConfigureAwait(false);

        Chat.AddMessageGetAll(addedMessage);

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

        /* When */
        var result = await ExecuteSubscribeAsync(new ExecutionOptions
        {
            Query = "subscription newMessageContent { newMessageContent }",
        }).ConfigureAwait(false);

        Chat.AddMessage(addedMessage);

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

        /* When */
        var result = await ExecuteSubscribeAsync(new ExecutionOptions
        {
            Query = "subscription MessageAdded { messageAdded { from { id displayName } content sentAt } }",
        }).ConfigureAwait(false);

        Chat.AddMessage(addedMessage);

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

        /* When */
        var result = await ExecuteSubscribeAsync(new ExecutionOptions
        {
            Query = "subscription MessageAdded { messageAddedAsync { from { id displayName } content sentAt } }",
        }).ConfigureAwait(false);

        Chat.AddMessage(addedMessage);

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

        /* When */
        var result = await ExecuteSubscribeAsync(new ExecutionOptions
        {
            Query = "subscription MessageAddedByUser($id:String!) { messageAddedByUser(id: $id) { from { id displayName } content sentAt } }",
            Variables = new Inputs(new Dictionary<string, object>
            {
                ["id"] = "1"
            })
        }).ConfigureAwait(false);

        Chat.AddMessage(addedMessage);

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

        /* When */
        var result = await ExecuteSubscribeAsync(new ExecutionOptions
        {
            Query = "subscription MessageAddedByUser($id:String!) { messageAddedByUserAsync(id: $id) { from { id displayName } content sentAt } }",
            Variables = new Inputs(new Dictionary<string, object>
            {
                ["id"] = "1"
            })
        }).ConfigureAwait(false);

        Chat.AddMessage(addedMessage);

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
        /* When */
        var result = await ExecuteSubscribeAsync(new ExecutionOptions
        {
            Query = "subscription MessageAdded { messageAdded { from { id displayName } content sentAt } }",
        }).ConfigureAwait(false);

        Chat.AddError(new Exception("test"));

        /* Then */
        var stream = result.Streams.Values.FirstOrDefault();
        var error = await Should.ThrowAsync<ExecutionError>(async () => await stream.FirstOrDefaultAsync()).ConfigureAwait(false);
        error.InnerException.Message.ShouldBe("test");
        error.Path.ShouldBe(new[] { "messageAdded" });
    }
}
