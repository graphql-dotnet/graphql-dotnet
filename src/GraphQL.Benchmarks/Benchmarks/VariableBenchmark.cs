using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.Extensions.DependencyInjection;
using GraphQL.SystemTextJson;

namespace GraphQL.Benchmarks
{
    [MemoryDiagnoser]
    //[RPlotExporter, CsvMeasurementsExporter]
    public class VariableBenchmark : IBenchmark
    {
        private IServiceProvider _provider;
        private ISchema _schema;
        private DocumentExecuter _executer;

        private Language.AST.Document _queryLiteralDocument;

        private Language.AST.Document _queryVariableDocument;
        private Inputs _variableInputs;

        private Language.AST.Document _queryDefaultVariableDocument;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var services = new ServiceCollection();

            services.AddSingleton<MyQueryGraphType>();
            services.AddSingleton<MyInputObjectGraphType>();
            services.AddSingleton<MySubInputObjectGraphType>();
            services.AddSingleton<ISchema, MySchema>();

            _provider = services.BuildServiceProvider();
            _schema = _provider.GetRequiredService<ISchema>();
            _schema.Initialize();
            _executer = new DocumentExecuter();
            _queryLiteralDocument = new Execution.GraphQLDocumentBuilder().Build(Queries.VariablesLiteral);

            _queryDefaultVariableDocument = new Execution.GraphQLDocumentBuilder().Build(Queries.VariablesDefaultVariable);

            _queryVariableDocument = new Execution.GraphQLDocumentBuilder().Build(Queries.VariablesVariable);
            _variableInputs = Variables.VariablesVariable.ToInputs();

            //confirm no errors during execution
            var val = EnableValidation;
            EnableValidation = true;
            Literal();
            DefaultVariable();
            Variable();
            EnableValidation = val;
        }

        public IEnumerable<bool> TrueFalse => new[] { true, false };

        [ParamsSource(nameof(TrueFalse))]
        public bool EnableValidation { get; set; }

        [Benchmark]
        public void Literal()
        {
            var result = ExecuteQuery(_schema, Queries.VariablesLiteral, _queryLiteralDocument, null);
        }

        [Benchmark]
        public void DefaultVariable()
        {
            var result = ExecuteQuery(_schema, Queries.VariablesDefaultVariable, _queryDefaultVariableDocument, null);
        }

        [Benchmark]
        public void Variable()
        {
            var result = ExecuteQuery(_schema, Queries.VariablesVariable, _queryVariableDocument, _variableInputs);
        }

        private ExecutionResult ExecuteQuery(ISchema schema, string query, Language.AST.Document document, Inputs inputs)
        {
            return _executer.ExecuteAsync(_ =>
            {
                _.Schema = schema;
                _.Query = query;
                _.Document = document;
                _.Inputs = inputs;
                _.ValidationRules = EnableValidation ? null : Array.Empty<IValidationRule>();
                _.ThrowOnUnhandledException = true;
            }).GetAwaiter().GetResult();
        }

        private ExecutionResult ExecuteQuery(ISchema schema, string query)
        {
            return _executer.ExecuteAsync(_ =>
            {
                _.Schema = schema;
                _.Query = query;
            }).GetAwaiter().GetResult();
        }

        void IBenchmark.Run() => Literal();

        public class MySchema : Schema
        {
            public MySchema()
            {
                Query = new MyQueryGraphType();
            }
        }

        public class MyQueryGraphType : ObjectGraphType
        {
            public MyQueryGraphType()
            {
                Field(
                    typeof(StringGraphType),
                    "test",
                    arguments: new QueryArguments(
                        new QueryArgument(typeof(NonNullGraphType<ListGraphType<MyInputObjectGraphType>>)) { Name = "inputs" }),
                    resolve: context => {
                        var arg = context.GetArgument<IList<MyInputObject>>("inputs");
                        if (arg.Count != 3)
                            throw new Exception();
                        if (arg[0].Ints.Length != 3)
                            throw new Exception();
                        if (arg[0].Ints[0].Length != 2)
                            throw new Exception();
                        if (arg[0].Widgets.Count != 2)
                            throw new Exception();
                        if (arg[0].Widgets[0].Description != "this is a test")
                            throw new Exception();
                        return "OK";
                        });
            }
        }

        public class MyInputObject
        {
            public int[][] Ints { get; set; }
            public IList<MySubInputObject> Widgets { get; set; }
        }

        public class MySubInputObject
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public double Amount { get; set; }
            public int Quantity { get; set; }
        }

        public class MyInputObjectGraphType : InputObjectGraphType<MyInputObject>
        {
            public MyInputObjectGraphType()
            {
                Name = "MyInputObject";
                Field(x => x.Ints, type: typeof(NonNullGraphType<ListGraphType<NonNullGraphType<ListGraphType<NonNullGraphType<IntGraphType>>>>>));
                Field(x => x.Widgets, type: typeof(NonNullGraphType<ListGraphType<MySubInputObjectGraphType>>));
            }
        }

        public class MySubInputObjectGraphType : InputObjectGraphType<MySubInputObject>
        {
            public MySubInputObjectGraphType()
            {
                Name = "MySubInputObject";
                Field(x => x.Name);
                Field(x => x.Description, true);
                Field(x => x.Amount);
                Field(x => x.Quantity);
            }
        }
    }
}
