using GraphQL.Conversion;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using ServiceLifetime = GraphQL.DI.ServiceLifetime;

namespace GraphQL.Tests;

public class QueryTestBase<TSchema> : QueryTestBase<TSchema, GraphQLDocumentBuilder>
   where TSchema : Schema
{
}

public class QueryTestBase<TSchema, TDocumentBuilder>
    where TSchema : Schema
    where TDocumentBuilder : IDocumentBuilder, new()
{
    public QueryTestBase()
    {
        Executer = new DocumentExecuter(new TDocumentBuilder(), new DocumentValidator());
    }

#pragma warning disable xUnit1013 // public method should be marked as test
    // WARNING: it is not static only for discoverability
    // WARNING: do not set any instance data inside
    // WARNING: method works on temporaly created instance
    public virtual void RegisterServices(IServiceRegister register)
    {
        register.TryRegister(typeof(TSchema), typeof(TSchema), ServiceLifetime.Singleton);
    }
#pragma warning restore xUnit1013 // public method should be marked as test

    // 1. get is not used by any test
    // 2. set is needed for some tests like MultithreadedTests/ComplexityTestBase that create an instance of test class manually
    public IServiceProvider ServiceProvider
    {
        private get => field ??= CreateServiceProvider();
        set;
    }

    private IServiceProvider CreateServiceProvider()
    {
        var collection = new ServiceCollection();
        collection.AddGraphQL(b => RegisterServices(b.Services));
        return collection.BuildServiceProvider();
    }

    public TSchema Schema => ServiceProvider.GetService<TSchema>() ?? throw new InvalidOperationException("Schema was not specified in DI container");

    public IDocumentExecuter Executer { get; }

    public bool AllWriters { get; set; } = true;

    public ExecutionResult AssertQuerySuccess(
        string query,
        string expected,
        Inputs? variables = null,
        object? root = null,
        IDictionary<string, object?>? userContext = null,
        CancellationToken cancellationToken = default,
        IEnumerable<IValidationRule>? rules = null,
        INameConverter? nameConverter = null,
        bool suppressSerializeExpected = false)
    {
        object queryResult = suppressSerializeExpected ? expected : CreateQueryResult(expected);
        return AssertQuery(query, queryResult, variables, root, userContext, cancellationToken, rules, null, nameConverter);
    }

    public ExecutionResult AssertQueryWithErrors(
        string query,
        string? expected,
        Inputs? variables = null,
        object? root = null,
        IDictionary<string, object?>? userContext = null,
        CancellationToken cancellationToken = default,
        IEnumerable<IValidationRule>? rules = null,
        int expectedErrorCount = 0,
        bool renderErrors = false,
        Func<UnhandledExceptionContext, Task>? unhandledExceptionDelegate = null,
        bool executed = true)
    {
        var queryResult = CreateQueryResult(expected, executed: executed);
        return AssertQueryIgnoreErrors(
            query,
            queryResult,
            variables,
            root,
            userContext,
            cancellationToken,
            rules,
            expectedErrorCount,
            renderErrors,
            unhandledExceptionDelegate);
    }

    public Task<ExecutionResult> AssertQueryWithErrorsAsync(
        string query,
        string? expected,
        Inputs? variables = null,
        object? root = null,
        IDictionary<string, object?>? userContext = null,
        CancellationToken cancellationToken = default,
        IEnumerable<IValidationRule>? rules = null,
        int expectedErrorCount = 0,
        bool renderErrors = false,
        Func<UnhandledExceptionContext, Task>? unhandledExceptionDelegate = null,
        bool executed = true)
    {
        var queryResult = CreateQueryResult(expected, executed: executed);
        return AssertQueryIgnoreErrorsAsync(
            query,
            queryResult,
            variables,
            root,
            userContext,
            cancellationToken,
            rules,
            expectedErrorCount,
            renderErrors,
            unhandledExceptionDelegate);
    }

    public ExecutionResult AssertQueryIgnoreErrors(
        string query,
        ExecutionResult expectedExecutionResult,
        Inputs? variables = null,
        object? root = null,
        IDictionary<string, object?>? userContext = null,
        CancellationToken cancellationToken = default,
        IEnumerable<IValidationRule>? rules = null,
        int expectedErrorCount = 0,
        bool renderErrors = false,
        Func<UnhandledExceptionContext, Task>? unhandledExceptionDelegate = null)
    {
        return AssertQueryIgnoreErrorsAsync(
            query, expectedExecutionResult, variables, root,
            userContext, cancellationToken, rules,
            expectedErrorCount, renderErrors, unhandledExceptionDelegate)
            .GetAwaiter()
            .GetResult();
    }

    public async Task<ExecutionResult> AssertQueryIgnoreErrorsAsync(
        string query,
        ExecutionResult expectedExecutionResult,
        Inputs? variables = null,
        object? root = null,
        IDictionary<string, object?>? userContext = null,
        CancellationToken cancellationToken = default,
        IEnumerable<IValidationRule>? rules = null,
        int expectedErrorCount = 0,
        bool renderErrors = false,
        Func<UnhandledExceptionContext, Task>? unhandledExceptionDelegate = null)
    {
        var schema = Schema;
        var runResult = await Executer.ExecuteAsync(options =>
        {
            options.Schema = Schema;
            options.Query = query;
            options.Root = root;
            options.Variables = variables;
            options.UserContext = userContext ?? new Dictionary<string, object?>();
            options.CancellationToken = cancellationToken;
            options.ValidationRules = rules;
            options.UnhandledExceptionDelegate = unhandledExceptionDelegate ?? (_ => Task.CompletedTask);
        }).ConfigureAwait(false);

        var renderResult = renderErrors ? runResult : new ExecutionResult { Data = runResult.Data, Executed = runResult.Executed };

        var writers = AllWriters ? GraphQLSerializersTestData.AllWriters : GraphQLSerializersTestData.AllNonAotWriters;

        foreach (var writer in writers)
        {
            string writtenResult = writer.Serialize(renderResult);
            expectedExecutionResult.Data = expectedExecutionResult.Data == null ? null :
                writer.Deserialize<Inputs>(new SystemTextJson.GraphQLSerializer().Serialize(expectedExecutionResult.Data));

            string expectedResult = writer.Serialize(expectedExecutionResult);

            writtenResult.ShouldBeCrossPlat(expectedResult);

            var errors = runResult.Errors ?? [];

            errors.Count.ShouldBe(expectedErrorCount);
        }

        return runResult;
    }

    public ExecutionResult AssertQuery(
        string query,
        object expectedExecutionResultOrJson,
        Inputs? variables,
        object? root,
        IDictionary<string, object?>? userContext = null,
        CancellationToken cancellationToken = default,
        IEnumerable<IValidationRule>? rules = null,
        Func<UnhandledExceptionContext, Task>? unhandledExceptionDelegate = null,
        INameConverter? nameConverter = null)
    {
        var schema = Schema;
        schema.NameConverter = nameConverter ?? CamelCaseNameConverter.Instance;
        var runResult = Executer.ExecuteAsync(options =>
        {
            options.Schema = schema;
            options.Query = query;
            options.Root = root;
            options.Variables = variables;
            options.UserContext = userContext ?? new Dictionary<string, object?>();
            options.CancellationToken = cancellationToken;
            options.ValidationRules = rules;
            options.UnhandledExceptionDelegate = unhandledExceptionDelegate ?? (_ => Task.CompletedTask);
            options.RequestServices = ServiceProvider;
        }).GetAwaiter().GetResult();

        var writers = AllWriters ? GraphQLSerializersTestData.AllWriters : GraphQLSerializersTestData.AllNonAotWriters;

        foreach (var writer in writers)
        {
            string writtenResult = writer.Serialize(runResult);
            string expectedResult = expectedExecutionResultOrJson is string s ? s : writer.Serialize((ExecutionResult)expectedExecutionResultOrJson);

            string additionalInfo = $"{writer.GetType().FullName} failed: ";

            if (runResult.Errors?.Any() == true)
            {
                additionalInfo += string.Join(Environment.NewLine, runResult.Errors
                    .Where(x => x.InnerException is GraphQLSyntaxErrorException)
                    .Select(x => x.InnerException!.Message));
            }

            writtenResult.ShouldBeCrossPlat(expectedResult, additionalInfo);
        }

        return runResult;
    }

    public static ExecutionResult CreateQueryResult(string? result, ExecutionErrors? errors = null, bool executed = true)
        => result.ToExecutionResult(errors, executed);
}
