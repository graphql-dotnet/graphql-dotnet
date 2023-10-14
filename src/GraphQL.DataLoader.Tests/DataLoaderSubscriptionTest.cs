using System.Reactive.Linq;
using System.Reactive.Subjects;
using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GraphQL.DataLoader.Tests;

public class DataLoaderSubscriptionTest : QueryTestBase
{
    protected async Task<ExecutionResult> ExecuteSubscribeAsync(string query)
    {
        var result = await ExecuteQueryAsync<DataLoaderTestSchema>(query).ConfigureAwait(false);
        result.Data.ShouldBeNull();
        return result;
    }

    [Fact]
    public async Task OneResultOverSubscription_Works()
    {
        var order = Fake.Orders.Generate();
        var ordersMock = Services.GetRequiredService<Mock<IOrdersStore>>();
        var orderStream = new ReplaySubject<Order>(1);

        ordersMock.Setup(x => x.GetOrderObservable()).Returns(orderStream);
        orderStream.OnNext(order);

        var result = await ExecuteSubscribeAsync("subscription OrderAdded { orderAdded { orderId } }").ConfigureAwait(false);

        /* Then */
        var stream = result.Streams.Values.FirstOrDefault();
        var message = await stream.FirstOrDefaultAsync();

        ordersMock.Verify(x => x.GetOrderObservable(), Times.Once);
        ordersMock.VerifyNoOtherCalls();

        message.Data.ShouldNotBeNull();
    }
}
