#nullable enable

namespace GraphQL.Tests.Subscription;

public static class ShouldlyExtensions
{
    public static ExecutionResult ShouldHaveResult(this SampleObserver? observer)
    {
        observer.ShouldNotBeNull();
        observer.Events.ShouldNotBeNull();
        observer.Events.TryDequeue(out var result).ShouldBeTrue("Observable sequence should have another result but did not");
        return result!;
    }

    public static void ShouldHaveNoMoreResults(this SampleObserver? observer)
    {
        observer.ShouldNotBeNull();
        observer.Events.ShouldNotBeNull();
        observer.Events.TryDequeue(out var result).ShouldBeFalse("Observable sequence should not have additional results but did");
    }

    public static ExecutionResult ShouldNotBeSuccessful(this ExecutionResult result)
    {
        if (result.Operation?.Operation == GraphQLParser.AST.OperationType.Subscription)
        {
            result.Data.ShouldBeNull();
        }
        result.Streams.ShouldBeNull();
        result.Errors.ShouldNotBeNull();
        result.Errors.Count.ShouldBeGreaterThan(0);
        return result;
    }

    public static ExecutionResult ShouldBeSuccessful(this ExecutionResult result)
    {
        if (result.Operation?.Operation == GraphQLParser.AST.OperationType.Subscription)
        {
            result.Streams.ShouldNotBeNull().Count.ShouldBe(1);
            result.Data.ShouldBeNull();
        }
        else
        {
            result.Data.ShouldNotBeNull();
        }
        result.Errors.ShouldBeNull();
        result.Executed.ShouldBeTrue();
        return result;
    }
}
