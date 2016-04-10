﻿using System;
using System.Linq;
using System.Threading;
using GraphQL.Execution;
using GraphQL.Http;
using GraphQL.Types;
using GraphQL.Validation;
using Newtonsoft.Json.Linq;
using Should;

namespace GraphQL.Tests
{
    public class QueryTestBase<TSchema> : QueryTestBase<TSchema, AntlrDocumentBuilder>
        where TSchema : ISchema
    {
    }

    public class QueryTestBase<TSchema, TDocumentBuilder>
        where TSchema : ISchema
        where TDocumentBuilder : IDocumentBuilder, new()
    {
        public QueryTestBase()
        {
            Services = new SimpleContainer();
            Executer = new DocumentExecuter(new TDocumentBuilder(), new DocumentValidator());
            Writer = new DocumentWriter(indent: true);
        }

        public ISimpleContainer Services { get; set; }

        public TSchema Schema
        {
            get { return Services.Get<TSchema>(); }
        }

        public IDocumentExecuter Executer { get; private set; }

        public IDocumentWriter Writer { get; private set; }

        public ExecutionResult AssertQuerySuccess(
            string query,
            string expected,
            Inputs inputs = null,
            object root = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQuery(query, queryResult, inputs, root, cancellationToken);
        }

        public ExecutionResult AssertQueryWithErrors(
            string query,
            string expected,
            Inputs inputs = null,
            object root = null,
            CancellationToken cancellationToken = default(CancellationToken),
            int expectedErrorCount = 0)
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQueryIgnoreErrors(query, queryResult, inputs, root, cancellationToken, expectedErrorCount);
        }

        public ExecutionResult AssertQueryIgnoreErrors(
            string query,
            ExecutionResult expectedExecutionResult,
            Inputs inputs,
            object root,
            CancellationToken cancellationToken = default(CancellationToken),
            int expectedErrorCount = 0)
        {
            var runResult = Executer.ExecuteAsync(Schema, root, query, null, inputs, cancellationToken).Result;

            var writtenResult = Writer.Write(new ExecutionResult {Data = runResult.Data});
            var expectedResult = Writer.Write(expectedExecutionResult);

            Console.WriteLine(writtenResult);

            writtenResult.ShouldEqual(expectedResult);

            var errors = runResult.Errors ?? new ExecutionErrors();

            errors.Count().ShouldEqual(expectedErrorCount);

            return runResult;
        }

        public ExecutionResult AssertQuery(string query, ExecutionResult expectedExecutionResult, Inputs inputs, object root, CancellationToken cancellationToken = default(CancellationToken))
        {
            var runResult = Executer.ExecuteAsync(Schema, root, query, null, inputs, cancellationToken).Result;

            var writtenResult = Writer.Write(runResult);
            var expectedResult = Writer.Write(expectedExecutionResult);

            Console.WriteLine(writtenResult);

            writtenResult.ShouldEqual(expectedResult);

            return runResult;
        }

        public ExecutionResult CreateQueryResult(string result)
        {
            var expected = JObject.Parse(result);
            return new ExecutionResult { Data = expected };
        }

        public ExecutionResult CreateErrorQueryResult()
        {
            return new ExecutionResult();
        }
    }
}
