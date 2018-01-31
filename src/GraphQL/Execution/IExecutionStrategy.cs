using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    public interface IExecutionStrategy
    {
        Task<ExecutionResult> ExecuteAsync(ExecutionContext context);
    }
}
