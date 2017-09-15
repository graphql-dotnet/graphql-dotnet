using System.Threading.Tasks;

namespace GraphQL.Subscription
{
    public interface ISubscriptionExecuter
    {
        Task<SubscriptionExecutionResult> SubscribeAsync(ExecutionOptions config);
    }
}
