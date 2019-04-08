using System;
using System.Linq;
using GraphQL.Http;
using GraphQL.Utilities;
using GraphQLParser.Exceptions;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace GraphQL.Tests.Utilities
{
    public class SchemaBuilderTestBase
    {
        public SchemaBuilderTestBase()
        {
            Builder = new SchemaBuilder();
        }

        protected readonly IDocumentExecutor Executor = new DocumentExecutor();
        protected readonly IDocumentWriter Writer = new DocumentWriter(indent: true);
        protected SchemaBuilder Builder { get; set; }

        public ExecutionResult AssertQuery(Action<ExecuteConfig> configure)
        {
            var config = new ExecuteConfig();
            configure(config);

            var schema = Builder.Build(config.Definitions);
            schema.Initialize();

            var queryResult = CreateQueryResult(config.ExpectedResult);

            return AssertQuery(_ =>
            {
                _.Schema = schema;
                _.Query = config.Query;
                _.Inputs = config.Variables.ToInputs();
                _.Root = config.Root;
                _.ThrowOnUnhandledException = config.ThrowOnUnhandledException;
            }, queryResult);
        }

        public ExecutionResult AssertQuery(Action<ExecutionOptions> options, ExecutionResult expectedExecutionResult)
        {
            var runResult = Executor.ExecuteAsync(options).Result;

            var writtenResult = Writer.WriteToStringAsync(runResult).Result;
            var expectedResult = Writer.WriteToStringAsync(expectedExecutionResult).Result;

            string additionalInfo = null;

            if (runResult.Errors?.Any() == true)
            {
                additionalInfo = string.Join(Environment.NewLine, runResult.Errors
                    .Where(x => x.InnerException is GraphQLSyntaxErrorException)
                    .Select(x => x.InnerException.Message));
            }

            writtenResult.ShouldBe(expectedResult, additionalInfo);

            return runResult;
        }

        public ExecutionResult CreateQueryResult(string result)
        {
            object expected = null;
            if (!string.IsNullOrWhiteSpace(result))
            {
                expected = JObject.Parse(result);
            }
            return new ExecutionResult { Data = expected };
        }
    }

    public class ExecuteConfig
    {
        public string Definitions { get; set; }
        public string Query { get; set; }
        public string Variables { get; set; }
        public string ExpectedResult { get; set; }
        public object Root { get; set; }
        public bool ThrowOnUnhandledException { get; set; }
    }
}
