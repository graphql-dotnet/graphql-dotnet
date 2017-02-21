using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Instrumentation;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    /// <summary>
    /// This class adds a variable to count the calls to ApplyTo()
    /// in the FieldMiddlewareBuilder class
    /// </summary>
    public class ApplyCounterMiddlewareBuilder : GraphQL.Instrumentation.IFieldMiddlewareBuilder
    {
        public int AppliedCount;
        IFieldMiddlewareBuilder overriddenBuilder = new FieldMiddlewareBuilder();
        public void ApplyTo(ISchema schema)
        {
            AppliedCount++;
            overriddenBuilder.ApplyTo(schema);
        }

        public FieldMiddlewareDelegate Build(FieldMiddlewareDelegate start = null)
        {
            return overriddenBuilder.Build(start);
        }

        public IFieldMiddlewareBuilder Use(Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate> middleware)
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
            var execOptions = new ExecutionOptions();
            execOptions.Schema = new Schema();
            var mockMiddleware = new ApplyCounterMiddlewareBuilder();
            execOptions.FieldMiddleware = mockMiddleware;

            // no execute in this test
            //docExec.ExecuteAsync(execOptions).Wait();

            Assert.Equal(0, mockMiddleware.AppliedCount);
        }
        [Fact]
        public void apply_to_called_once()
        {
            var docExec = new DocumentExecuter();
            var execOptions = new ExecutionOptions();
            execOptions.Schema = new Schema();
            var mockMiddleware = new ApplyCounterMiddlewareBuilder();
            execOptions.FieldMiddleware = mockMiddleware;

            docExec.ExecuteAsync(execOptions).Wait();

            Assert.Equal(1, mockMiddleware.AppliedCount);
        }
        [Fact]
        public void apply_to_called_once_with_multiple_execute()
        {
            var docExec = new DocumentExecuter();
            var execOptions = new ExecutionOptions();
            execOptions.Schema = new Schema();
            var mockMiddleware = new ApplyCounterMiddlewareBuilder();
            execOptions.FieldMiddleware = mockMiddleware;

            docExec.ExecuteAsync(execOptions).Wait();
            docExec.ExecuteAsync(execOptions).Wait();

            Assert.Equal(1, mockMiddleware.AppliedCount);
        }
    }
}
