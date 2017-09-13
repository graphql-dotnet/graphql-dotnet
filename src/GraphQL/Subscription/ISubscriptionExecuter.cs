using System.Threading.Tasks;
using GraphQL.Execution;

namespace GraphQL.Subscription
{
    public interface ISubscriptionExecuter : IDocumentExecuter
    {
        Task<SubscriptionExecutionResult> SubscribeAsync(ExecutionOptions config);
    }
}
