#nullable enable

using GraphQL.Subscription;
using GraphQL.SystemTextJson;

namespace GraphQL.Tests.Subscription;

public static class ShouldlyExtensions
{
    private static readonly IGraphQLTextSerializer _serializer = new GraphQLSerializer();

    public static void ShouldBeSimilarTo(this object? actual, object? expected, string? customMessage = null)
    {
        if (expected is string str)
            expected = _serializer.Deserialize<Inputs>(str);
        var expectedJson = _serializer.Serialize(expected);
        var actualJson = _serializer.Serialize(actual);
        actualJson.ShouldBe(expectedJson, customMessage);
    }

    public static void ShouldBeSuccessful(this SubscriptionExecutionResult actual)
    {
        actual.ShouldNotBeNull();
        actual.Data.ShouldBeNull();
        actual.Errors.ShouldBeNull();
        actual.Streams.ShouldNotBeNull();
        actual.Streams.Count.ShouldBe(1);
    }

    public static ExecutionResult ShouldHaveResult(this SubscriptionExecutionStrategyTests.SampleObserver? observer)
    {
        observer.ShouldNotBeNull();
        observer.Events.ShouldNotBeNull();
        observer.Events.TryDequeue(out var result).ShouldBeTrue("Observable sequence should have another result but did not");
        return result;
    }

    public static void ShouldHaveNoMoreResults(this SubscriptionExecutionStrategyTests.SampleObserver? observer)
    {
        observer.ShouldNotBeNull();
        observer.Events.ShouldNotBeNull();
        observer.Events.TryDequeue(out var result).ShouldBeFalse("Observable sequence should not have additional results but did");
    }

    public static ExecutionResult ShouldBeSuccessful(this ExecutionResult result)
    {
        result.Data.ShouldNotBeNull();
        result.Errors.ShouldBeNull();
        return result;
    }
}
