using GraphQL.Caching;
using GraphQL.Transport;
using GraphQLParser.AST;

namespace GraphQL.PersistedQueries;

/// <summary>
/// .
/// </summary>
public class PersistedQueriesExecutor : IPersistedQueriesExecutor
{
    private readonly IDocumentCache _cache;

    /// <summary>
    /// .
    /// </summary>
    /// <param name="cache"></param>
    public PersistedQueriesExecutor(IDocumentCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// .
    /// </summary>
    /// <param name="gqlRequest"></param>
    /// <param name="userContext"></param>
    /// <param name="executer"></param>
    /// <param name="requestServices"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public virtual async Task<ExecutionResult> ExecuteRequestAsync(
        GraphQLRequest gqlRequest,
        IDictionary<string, object?> userContext,
        IDocumentExecuter executer,
        IServiceProvider requestServices,
        CancellationToken token)
    {
        ExecutionResult result;
        if (!string.IsNullOrWhiteSpace(gqlRequest.Hash))
        {
            if (string.IsNullOrWhiteSpace(gqlRequest.Query))
            {
                var document = await _cache.GetAsync(gqlRequest.Hash!).ConfigureAwait(false);

                if (document == null)
                {
                    result = new ExecutionResult
                    {
                        Errors = new ExecutionErrors
                        {
                            new ExecutionError($"A persisted query with \"{gqlRequest.Hash}\" hash hasn't been found. Add the query and repeat the request.")
                        }
                    };
                }
                else
                {
                    result = await ExecuteRequestAsync(gqlRequest, document, userContext, executer, requestServices, token).ConfigureAwait(false);
                }
            }
            else
            {
                result = await ExecuteRequestAsync(gqlRequest, null, userContext, executer, requestServices, token).ConfigureAwait(false);

                if (result.Document != null)
                {
                    await _cache.SetByHashAsync(gqlRequest.Hash!, gqlRequest.Query!, result.Document).ConfigureAwait(false);
                }
            }
        }
        else
        {
            result = await ExecuteRequestAsync(gqlRequest, null, userContext, executer, requestServices, token).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// .
    /// </summary>
    /// <param name="gqlRequest"></param>
    /// <param name="graphQLDocument"></param>
    /// <param name="userContext"></param>
    /// <param name="executer"></param>
    /// <param name="requestServices"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    protected virtual Task<ExecutionResult> ExecuteRequestAsync(
        GraphQLRequest gqlRequest,
        GraphQLDocument? graphQLDocument,
        IDictionary<string, object?> userContext,
        IDocumentExecuter executer,
        IServiceProvider requestServices,
        CancellationToken token)
        => executer.ExecuteAsync(new ExecutionOptions
        {
            Query = graphQLDocument == null ? gqlRequest.Query : null,
            Document = graphQLDocument,
            OperationName = gqlRequest.OperationName,
            Variables = gqlRequest.Variables,
            Extensions = gqlRequest.Extensions,
            UserContext = userContext,
            RequestServices = requestServices,
            CancellationToken = token
        });
}
