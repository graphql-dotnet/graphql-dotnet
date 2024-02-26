using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.Exceptions;

namespace GraphQL.Tests;

public class BasicQueryTestBase
{
    protected readonly IDocumentExecuter Executer = new DocumentExecuter();
    protected readonly IGraphQLTextSerializer Writer = new GraphQLSerializer(indent: true);

    public ExecutionResult AssertQuerySuccess(
        ISchema schema,
        string query,
        string expected,
        Inputs? variables = null,
        object? root = null,
        IDictionary<string, object?>? userContext = null,
        CancellationToken cancellationToken = default,
        IEnumerable<IValidationRule>? rules = null)
    {
        var queryResult = CreateQueryResult(expected);
        return AssertQuery(schema, query, queryResult, variables, root, userContext, cancellationToken, rules);
    }

    public ExecutionResult AssertQuerySuccess(Action<ExecutionOptions> options, string expected)
    {
        var queryResult = CreateQueryResult(expected);
        return AssertQuery(options, queryResult);
    }

    public ExecutionResult AssertQuery(Action<ExecutionOptions> options, ExecutionResult expectedExecutionResult)
    {
        var runResult = Executer.ExecuteAsync(options).Result;

        string writtenResult = Writer.Serialize(runResult);
        string expectedResult = Writer.Serialize(expectedExecutionResult);

        //#if DEBUG
        //            Console.WriteLine(writtenResult);
        //#endif

        string? additionalInfo = null;

        if (runResult.Errors?.Any() == true)
        {
            additionalInfo = string.Join(Environment.NewLine, runResult.Errors
                .Where(x => x.InnerException is GraphQLSyntaxErrorException)
                .Select(x => x.InnerException!.Message));
        }

        writtenResult.ShouldBe(expectedResult, additionalInfo);

        return runResult;
    }

    public ExecutionResult AssertQuery(
        ISchema schema,
        string query,
        ExecutionResult expectedExecutionResult,
        Inputs? variables,
        object? root,
        IDictionary<string, object?>? userContext = null,
        CancellationToken cancellationToken = default,
        IEnumerable<IValidationRule>? rules = null)
    {
        var runResult = Executer.ExecuteAsync(_ =>
        {
            _.Schema = schema;
            _.Query = query;
            _.Root = root;
            _.Variables = variables;
            _.UserContext = userContext ?? new Dictionary<string, object?>();
            _.CancellationToken = cancellationToken;
            _.ValidationRules = rules;
        }).GetAwaiter().GetResult();

        string writtenResult = Writer.Serialize(runResult);
        string expectedResult = Writer.Serialize(expectedExecutionResult);

        //#if DEBUG
        //            Console.WriteLine(writtenResult);
        //#endif

        string? additionalInfo = null;

        if (runResult.Errors?.Any() == true)
        {
            additionalInfo = string.Join(Environment.NewLine, runResult.Errors
                .Where(x => x.InnerException is GraphQLSyntaxErrorException)
                .Select(x => x.InnerException!.Message));
        }

        writtenResult.ShouldBe(expectedResult, additionalInfo);

        return runResult;
    }

    public static ExecutionResult CreateQueryResult(string result) => result.ToExecutionResult();
}
