using System.Threading.Tasks;

namespace GraphQL.Execution
{
    public interface IExecutionStrategy
    {
        Task<ExecutionResult> ExecuteAsync(ExecutionContext context);
    }
}
