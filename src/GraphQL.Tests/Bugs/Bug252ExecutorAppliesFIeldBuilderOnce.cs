using GraphQL.Instrumentation;
using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

/// <summary>
/// This class adds a variable to count the calls to ApplyTo()
/// in the FieldMiddlewareBuilder class
/// </summary>
public class ApplyCounterMiddlewareBuilder : IFieldMiddlewareBuilder
{
    public int AppliedCount;
    private readonly FieldMiddlewareBuilder overriddenBuilder = new FieldMiddlewareBuilder();

    public Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate> Build()
    {
        AppliedCount++;
        return overriddenBuilder.Build();
    }

    public IFieldMiddlewareBuilder Use(Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate> middleware)
        => overriddenBuilder.Use(middleware);
}

public class Bug252ExecutorAppliesBuilderOnceTests
{
    [Fact]
    public void apply_to_not_called_without_execute()
    {
        //var docExec = new DocumentExecuter();
        var schema = new Schema();
        //var execOptions = new ExecutionOptions { Schema = schema };
        var mockMiddleware = new ApplyCounterMiddlewareBuilder();
        schema.FieldMiddleware = mockMiddleware;

        // no execute in this test
        //docExec.ExecuteAsync(execOptions).Wait();

        mockMiddleware.AppliedCount.ShouldBe(0);
    }

    [Fact]
    public void apply_to_called_once()
    {
        var docExec = new DocumentExecuter();
        var schema = new Schema();
        var execOptions = new ExecutionOptions
        {
            Schema = schema,
            Query = "{ abcd }"
        };
        var mockMiddleware = new ApplyCounterMiddlewareBuilder();
        schema.FieldMiddleware = mockMiddleware;

        docExec.ExecuteAsync(execOptions).Wait();

        mockMiddleware.AppliedCount.ShouldBe(1);
    }

    [Fact]
    public void apply_to_called_once_with_multiple_execute()
    {
        var docExec = new DocumentExecuter();
        var schema = new Schema();
        var execOptions = new ExecutionOptions
        {
            Schema = schema,
            Query = "{ abcd }"
        };
        var mockMiddleware = new ApplyCounterMiddlewareBuilder();
        schema.FieldMiddleware = mockMiddleware;

        docExec.ExecuteAsync(execOptions).Wait();
        docExec.ExecuteAsync(execOptions).Wait();

        mockMiddleware.AppliedCount.ShouldBe(1);
    }
}
