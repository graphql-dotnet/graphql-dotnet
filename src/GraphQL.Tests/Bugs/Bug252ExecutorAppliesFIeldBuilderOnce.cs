using GraphQL.Instrumentation;
using GraphQL.Types;
using Shouldly;
using System;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    /// <summary>
    /// This class adds a variable to count the calls to ApplyTo()
    /// in the FieldMiddlewareBuilder class
    /// </summary>
    public class ApplyCounterMiddlewareBuilder : IFieldMiddlewareBuilder
    {
        public int AppliedCount;
        private readonly FieldMiddlewareBuilder overriddenBuilder = new FieldMiddlewareBuilder();

        public void ApplyTo(ISchema schema)
        {
            AppliedCount++;
            overriddenBuilder.ApplyTo(schema);
        }

        public FieldMiddlewareDelegate Build(FieldMiddlewareDelegate start = null)
        {
            return overriddenBuilder.Build(null, start);
        }

        public IFieldMiddlewareBuilder Use(Func<ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate> middleware)
        {
            return overriddenBuilder.Use(middleware);
        }
    }

    public class Bug252ExecutorAppliesBuilderOnceTests
    {
        [Fact]
        public void apply_to_not_called_without_execute()
        {
            var docExec = new DocumentExecuter();
            var execOptions = new ExecutionOptions { Schema = new Schema() };
            var mockMiddleware = new ApplyCounterMiddlewareBuilder();
            execOptions.FieldMiddleware = mockMiddleware;

            // no execute in this test
            //docExec.ExecuteAsync(execOptions).Wait();

            mockMiddleware.AppliedCount.ShouldBe(0);
        }
        [Fact]
        public void apply_to_called_once()
        {
            var docExec = new DocumentExecuter();
            var execOptions = new ExecutionOptions
            {
                Schema = new Schema(),
                Query = "{ abcd }"
            };
            var mockMiddleware = new ApplyCounterMiddlewareBuilder();
            execOptions.FieldMiddleware = mockMiddleware;

            docExec.ExecuteAsync(execOptions).Wait();

            mockMiddleware.AppliedCount.ShouldBe(1);
        }

        [Fact]
        public void apply_to_called_once_with_multiple_execute()
        {
            var docExec = new DocumentExecuter();
            var execOptions = new ExecutionOptions
            {
                Schema = new Schema(),
                Query = "{ abcd }"
            };
            var mockMiddleware = new ApplyCounterMiddlewareBuilder();
            execOptions.FieldMiddleware = mockMiddleware;

            docExec.ExecuteAsync(execOptions).Wait();
            docExec.ExecuteAsync(execOptions).Wait();

            mockMiddleware.AppliedCount.ShouldBe(1);
        }
    }
}
