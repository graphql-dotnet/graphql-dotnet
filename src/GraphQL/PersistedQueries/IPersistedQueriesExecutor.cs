using GraphQL.Transport;

namespace GraphQL.PersistedQueries;

/// <summary>
/// .
/// </summary>
public interface IPersistedQueriesExecutor
{
    /// <summary>
    /// .
    /// </summary>
    /// <param name="gqlRequest"></param>
    /// <param name="userContext"></param>
    /// <param name="executer"></param>
    /// <param name="requestServices"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<ExecutionResult> ExecuteRequestAsync(
        GraphQLRequest gqlRequest,
        IDictionary<string, object?> userContext,
        IDocumentExecuter executer,
        IServiceProvider requestServices,
        CancellationToken token);
}
