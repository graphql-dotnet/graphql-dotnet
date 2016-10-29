using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using GraphQL.Http;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQL.Validation;
using GraphQLParser.Exceptions;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class BasicQueryTestBase
    {
        protected readonly IDocumentExecuter Executer = new DocumentExecuter();
        protected readonly IDocumentWriter Writer = new DocumentWriter(indent: true);

        public ExecutionResult AssertQuerySuccess(
            ISchema schema,
            string query,
            string expected,
            Inputs inputs = null,
            object root = null,
            object userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
            IEnumerable<IValidationRule> rules = null)
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQuery(schema, query, queryResult, inputs, root, userContext, cancellationToken, rules);
        }

        public ExecutionResult AssertQuery(
            ISchema schema,
            string query,
            ExecutionResult expectedExecutionResult,
            Inputs inputs,
            object root,
            object userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
            IEnumerable<IValidationRule> rules = null)
        {
            var runResult = Executer.ExecuteAsync(
                schema,
                root,
                query,
                null,
                inputs,
                userContext,
                cancellationToken,
                rules
                ).Result;

            var writtenResult = Writer.Write(runResult);
            var expectedResult = Writer.Write(expectedExecutionResult);

#if DEBUG
            Console.WriteLine(writtenResult);
#endif

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

    public class RegisteredInstanceTests : BasicQueryTestBase
    {
        [Fact]
        public void build_dynamic_schema()
        {
            var schema = new Schema();

            var person = new ObjectGraphType();
            person.Name = "Person";
            person.Field("name", new StringGraphType());
            person.Field(
                "friends",
                new ListGraphType(new NonNullGraphType(person)),
                resolve: ctx => new[] {new SomeObject {Name = "Jaime"}, new SomeObject {Name = "Joe"}});

            var root = new ObjectGraphType();
            root.Name = "Root";
            root.Field("hero", person, resolve: ctx => ctx.RootValue);

            schema.Query = root;
            schema.RegisterTypes(person);

            var printed = new SchemaPrinter(schema).Print();

#if DEBUG
            Console.WriteLine(printed);
#endif

            AssertQuerySuccess(
                schema,
                @"{ hero { name friends { name } } }",
                @"{ hero: { name : 'Quinn', friends: [ { name: 'Jaime' }, { name: 'Joe' }] } }",
                root: new SomeObject { Name = "Quinn"});
        }

        public class SomeObject
        {
            public string Name { get; set; }
        }
    }

    public static class ObjectGraphTypeExtensions
    {
        public static void Field(
            this IObjectGraphType obj,
            string name,
            IGraphType type,
            Func<ResolveFieldContext, object> resolve = null)
        {
            var field = new FieldType();
            field.Name = name;
            field.ResolvedType = type;
            field.Resolver = resolve != null ? new FuncFieldResolver<object>(resolve) : null;
            obj.AddField(field);
        }
    }
}
