#nullable enable

using System.Collections.Concurrent;

namespace GraphQL.Tests.Subscription;

public class SampleObserver : IObserver<ExecutionResult>
{
    public ConcurrentQueue<ExecutionResult> Events { get; } = new();
    public void OnCompleted() => throw new NotImplementedException("OnCompleted should not occur");
    public void OnError(Exception error) => throw new NotImplementedException("OnError should not occur");
    public void OnNext(ExecutionResult value) => Events.Enqueue(value);
}

