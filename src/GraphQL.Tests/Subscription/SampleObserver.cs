#nullable enable

using System.Collections.Concurrent;

namespace GraphQL.Tests.Subscription;

public class SampleObserver : IObserver<ExecutionResult>
{
    public ConcurrentQueue<ExecutionResult> Events { get; private set; } = new();
    public void OnCompleted() => Events = null!; //should not occur, and this will break the tests (throwing an exception here would not)
    public void OnError(Exception error)
    {
        if (error is ExecutionError executionError)
            Events.Enqueue(new ExecutionResult { Errors = new ExecutionErrors { executionError } });
        else
            Events.Enqueue(new ExecutionResult { Errors = new ExecutionErrors { new ExecutionError($"Unhandled error of type {error.GetType().Name}") } });
    }
    public void OnNext(ExecutionResult value) => Events.Enqueue(value);
}

