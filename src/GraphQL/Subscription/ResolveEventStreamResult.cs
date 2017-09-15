using System;

namespace GraphQL.Subscription
{
    public class ResolveEventStreamResult
    {
        public IObservable<ExecutionResult> Value { get; set; }

        public bool Skip { get; set; }
    }
}
