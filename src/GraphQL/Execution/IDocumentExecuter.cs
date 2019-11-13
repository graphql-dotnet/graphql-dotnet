using System;
using System.Threading.Tasks;

namespace GraphQL
{
    public interface IDocumentExecuter
    {
        Task<ExecutionResult> ExecuteAsync(ExecutionOptions options);

        Task<ExecutionResult> ExecuteAsync(Action<ExecutionOptions> configure);
    }
}
