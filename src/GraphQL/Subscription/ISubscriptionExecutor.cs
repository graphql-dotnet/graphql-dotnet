using System.Threading.Tasks;

namespace GraphQL.Subscription
{
    public interface ISubscriptionExecutor
    {
        Task<SubscriptionExecutionResult> SubscribeAsync(ExecutionOptions config);
    }
}
