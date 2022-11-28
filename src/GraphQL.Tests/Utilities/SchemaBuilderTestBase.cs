using GraphQL.Execution;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser.Exceptions;

namespace GraphQL.Tests.Utilities;

public class SchemaBuilderTestBase
{
    public SchemaBuilderTestBase()
    {
        Builder = new SchemaBuilder();
    }

    protected readonly IDocumentExecuter Executer = new DocumentExecuter();
    protected readonly IGraphQLTextSerializer Serializer = new GraphQLSerializer(indent: true);
    protected SchemaBuilder Builder { get; set; }

    public ExecutionResult AssertQuery(Action<ExecuteConfig> configure)
    {
        var config = new ExecuteConfig();
        configure(config);

        var schema = Builder.Build(config.Definitions);
        config.ConfigureBuildedSchema?.Invoke(schema);
        schema.Initialize();

        var queryResult = CreateQueryResult(config.ExpectedResult);

        return AssertQuery(
            _ =>
            {
                _.Schema = schema;
                _.Query = config.Query;
                _.Variables = config.Variables.ToInputs();
                _.Root = config.Root;
                _.ThrowOnUnhandledException = config.ThrowOnUnhandledException;
                _.Listeners.AddRange(config.Listeners);
            },
            queryResult);
    }

    public ExecutionResult AssertQuery(Action<ExecutionOptions> options, ExecutionResult expectedExecutionResult)
    {
        var runResult = Executer.ExecuteAsync(options).Result;

        var writtenResult = Serializer.Serialize(runResult);
        var expectedResult = Serializer.Serialize(expectedExecutionResult);

        string additionalInfo = null;

        if (runResult.Errors?.Any() == true)
        {
            additionalInfo = string.Join(Environment.NewLine, runResult.Errors
                .Where(x => x.InnerException is GraphQLSyntaxErrorException)
                .Select(x => x.InnerException.Message));
        }

        writtenResult.ShouldBeCrossPlat(expectedResult, additionalInfo);

        return runResult;
    }

    public ExecutionResult CreateQueryResult(string result) => result.ToExecutionResult();
}

public class ExecuteConfig
{
    public string Definitions { get; set; }
    public string Query { get; set; }
    public string Variables { get; set; }
    public string ExpectedResult { get; set; }
    public object Root { get; set; }
    public bool ThrowOnUnhandledException { get; set; }
    public List<IDocumentExecutionListener> Listeners { get; set; } = new List<IDocumentExecutionListener>();
    public Action<ISchema> ConfigureBuildedSchema { get; set; }
}
