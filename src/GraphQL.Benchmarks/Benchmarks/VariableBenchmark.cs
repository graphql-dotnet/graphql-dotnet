using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Benchmarks
{
    [MemoryDiagnoser]
    //[RPlotExporter, CsvMeasurementsExporter]
    public class VariableBenchmark : IBenchmark
    {
        private IServiceProvider _provider;
        private ISchema _schema;
        private DocumentExecuter _executer;

        private string _queryLiteral;
        private Language.AST.Document _queryLiteralDocument;

        private string _queryVariable;
        private Language.AST.Document _queryVariableDocument;
        private Inputs _variableInputs;

        private string _queryDefaultVariable;
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
            _queryLiteral = @"
{
  test(inputs: [
    {
      ints: [[1,2],[3,4,5],[6,7,8]],
      widgets: [
        {name:""bolts1"", description:""this is a test"", amount:2.99, quantity:42},
        {name:""bolts2"", description:""this is a test"", amount:2.99, quantity:42}
      ]
    },
    {
      ints: [[11,12],[13,14,15],[16,17,18]],
      widgets: [
        {name:""bolts3"", description:""this is a test"", amount:2.99, quantity:42},
        {name:""bolts4"", description:""this is a test"", amount:2.99, quantity:42}
      ]
    },
    {
      ints: [[21,12],[13,14,15],[16,17,18]],
      widgets: [
        {name:""bolts5"", description:""this is a test"", amount:2.99, quantity:42},
        {name:""bolts6"", description:""this is a test"", amount:2.99, quantity:42}
      ]
    }
  ])
}
";
            _queryLiteralDocument = new Execution.GraphQLDocumentBuilder().Build(_queryLiteral);

            _queryDefaultVariable = @"
query ($in: [MyInputObject] =
  [
    {
      ints: [[1,2],[3,4,5],[6,7,8]],
      widgets: [
        {name:""bolts1"", description:""this is a test"", amount:2.99, quantity:42},
        {name:""bolts2"", description:""this is a test"", amount:2.99, quantity:42}
      ]
    },
    {
      ints: [[11,12],[13,14,15],[16,17,18]],
      widgets: [
        {name:""bolts3"", description:""this is a test"", amount:2.99, quantity:42},
        {name:""bolts4"", description:""this is a test"", amount:2.99, quantity:42}
      ]
    },
    {
      ints: [[21,12],[13,14,15],[16,17,18]],
      widgets: [
        {name:""bolts5"", description:""this is a test"", amount:2.99, quantity:42},
        {name:""bolts6"", description:""this is a test"", amount:2.99, quantity:42}
      ]
    }
  ])
{
  test(inputs: $in)
}
";
            _queryDefaultVariableDocument = new Execution.GraphQLDocumentBuilder().Build(_queryDefaultVariable);

            _queryVariable = @"
query ($in: [MyInputObject])
{
  test(inputs: $in)
}
";
            _queryVariableDocument = new Execution.GraphQLDocumentBuilder().Build(_queryVariable);
            _variableInputs = ToInputs(@"
{ ""in"":
  [
    {
      ""ints"": [[1,2],[3,4,5],[6,7,8]],
      ""widgets"": [
        {""name"":""bolts1"", ""description"":""this is a test"", ""amount"":2.99, ""quantity"":42},
        {""name"":""bolts2"", ""description"":""this is a test"", ""amount"":2.99, ""quantity"":42}
      ]
    },
    {
      ""ints"": [[11,12],[13,14,15],[16,17,18]],
      ""widgets"": [
        {""name"":""bolts3"", ""description"":""this is a test"", ""amount"":2.99, ""quantity"":42},
        {""name"":""bolts4"", ""description"":""this is a test"", ""amount"":2.99, ""quantity"":42}
      ]
    },
    {
      ""ints"": [[21,12],[13,14,15],[16,17,18]],
      ""widgets"": [
        {""name"":""bolts5"", ""description"":""this is a test"", ""amount"":2.99, ""quantity"":42},
        {""name"":""bolts6"", ""description"":""this is a test"", ""amount"":2.99, ""quantity"":42}
      ]
    }
  ]
}
");
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
            var result = ExecuteQuery(_schema, _queryLiteral, _queryLiteralDocument, null);
        }

        [Benchmark]
        public void DefaultVariable()
        {
            var result = ExecuteQuery(_schema, _queryDefaultVariable, _queryDefaultVariableDocument, null);
        }

        [Benchmark]
        public void Variable()
        {
            var result = ExecuteQuery(_schema, _queryVariable, _queryVariableDocument, _variableInputs);
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

        private Inputs ToInputs(string inputs)
        {
            return SystemTextJson.StringExtensions.ToInputs(inputs);
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
