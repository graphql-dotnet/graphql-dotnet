using System;
using System.Linq;
using GraphQL.Http;
using GraphQL.Utilities;
using GraphQLParser.Exceptions;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace GraphQL.Tests.Utilities
{
    public class TypedSchemaBuilderTestBase
    {
        public TypedSchemaBuilderTestBase()
        {
            Builder = new TypedSchemaBuilder();
        }

        protected readonly IDocumentExecuter Executer = new DocumentExecuter();
        protected readonly IDocumentWriter Writer = new DocumentWriter(indent: true);
        protected TypedSchemaBuilder Builder { get; set; }

        public ExecutionResult AssertQuery(Action<TypedExecuteConfig> configure)
        {
            var config = new TypedExecuteConfig();
            configure(config);

            var schema = Builder.Build(config.Types);
            schema.Initialize();

            var queryResult = CreateQueryResult(config.ExpectedResult);

            return AssertQuery(_ =>
            {
                _.Schema = schema;
                _.Query = config.Query;
                _.Inputs = config.Variables.ToInputs();
            }, queryResult);
        }

        public ExecutionResult AssertQuery(Action<ExecutionOptions> options, ExecutionResult expectedExecutionResult)
        {
            var runResult = Executer.ExecuteAsync(options).Result;

            var writtenResult = Writer.Write(runResult);
            var expectedResult = Writer.Write(expectedExecutionResult);

            string additionalInfo = null;

            if (runResult.Errors?.Any() == true)
            {
                additionalInfo = string.Join(Environment.NewLine, runResult.Errors.Where(x => x.InnerException is GraphQLSyntaxErrorException)
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
}