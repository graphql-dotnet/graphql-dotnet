using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using GraphQL.Execution;
using GraphQL.Language;
using GraphQL.Language.AST;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Benchmarks
{
    [MemoryDiagnoser]
    [Config(typeof(BenchmarkConfig))]
    //[RPlotExporter, CsvMeasurementsExporter]
    public class DetailedBenchmark : IBenchmark
    {
        private class BenchmarkConfig : ManualConfig
        {
            public BenchmarkConfig()
            {
                Orderer = new GroupbyQueryOrderer();
            }

            private class GroupbyQueryOrderer : IOrderer
            {
                public IEnumerable<BenchmarkCase> GetExecutionOrder(ImmutableArray<BenchmarkCase> benchmarksCase) => benchmarksCase;
                public IEnumerable<BenchmarkCase> GetSummaryOrder(ImmutableArray<BenchmarkCase> benchmarksCase, Summary summary) => benchmarksCase;
                public string GetHighlightGroupKey(BenchmarkCase benchmarkCase) => null;
                public string GetLogicalGroupKey(ImmutableArray<BenchmarkCase> allBenchmarksCases, BenchmarkCase benchmarkCase) => benchmarkCase.Descriptor.WorkloadMethodDisplayInfo;
                public IEnumerable<IGrouping<string, BenchmarkCase>> GetLogicalGroupOrder(IEnumerable<IGrouping<string, BenchmarkCase>> logicalGroups) => logicalGroups;
                public bool SeparateLogicalGroups => true;
            }
        }

        private BenchmarkInfo _bIntrospection;
        private BenchmarkInfo _bHero;
        private BenchmarkInfo _bVariable;
        private BenchmarkInfo _bLiteral;
        private readonly DocumentExecuter _documentExecuter = new DocumentExecuter();

        [GlobalSetup]
        public void GlobalSetup()
        {
            Func<ISchema> starWarsSchemaBuilder = () =>
            {
                var services = new ServiceCollection();

                services.AddSingleton<StarWarsData>();
                services.AddSingleton<StarWarsQuery>();
                services.AddSingleton<StarWarsMutation>();
                services.AddSingleton<HumanType>();
                services.AddSingleton<HumanInputType>();
                services.AddSingleton<DroidType>();
                services.AddSingleton<CharacterInterface>();
                services.AddSingleton<EpisodeEnum>();
                services.AddSingleton<ISchema, StarWarsSchema>();

                var provider = services.BuildServiceProvider();
                var schema = provider.GetRequiredService<ISchema>();
                schema.Initialize();
                return schema;
            };

            _bIntrospection = new BenchmarkInfo(Queries.Introspection, null, starWarsSchemaBuilder);
            _bHero = new BenchmarkInfo(Queries.Hero, null, starWarsSchemaBuilder);

            Func<ISchema> variableSchemaBuilder = () =>
            {
                var services = new ServiceCollection();

                services.AddSingleton<VariableBenchmark.MyQueryGraphType>();
                services.AddSingleton<VariableBenchmark.MyInputObjectGraphType>();
                services.AddSingleton<VariableBenchmark.MySubInputObjectGraphType>();
                services.AddSingleton<ISchema, VariableBenchmark.MySchema>();

                var provider = services.BuildServiceProvider();
                var schema = provider.GetRequiredService<ISchema>();
                schema.Initialize();
                return schema;
            };

            _bVariable = new BenchmarkInfo(Queries.VariablesVariable, Benchmarks.Variables.VariablesVariable, variableSchemaBuilder);
            _bLiteral = new BenchmarkInfo(Queries.VariablesLiteral, null, variableSchemaBuilder);
        }

        [Benchmark]
        public void Introspection()
        {
            Run(_bIntrospection);
        }

        [Benchmark]
        public void Hero()
        {
            Run(_bHero);
        }

        [Benchmark]
        public void Variables()
        {
            Run(_bVariable);
        }

        [Benchmark]
        public void Literal()
        {
            Run(_bLiteral);
        }

        private void Run(BenchmarkInfo benchmarkInfo)
        {
            switch (Stage)
            {
                case StageEnum.Build:
                    benchmarkInfo.BuildSchema();
                    break;
                case StageEnum.TypicalExecution:
                    _documentExecuter.ExecuteAsync(o =>
                    {
                        o.Schema = benchmarkInfo.Schema;
                        o.Query = benchmarkInfo.Query;
                        o.Inputs = benchmarkInfo.InputsString?.ToInputs();
                    }).GetAwaiter().GetResult();
                    break;
                case StageEnum.Parse:
                    benchmarkInfo.Parse();
                    break;
                case StageEnum.Convert:
                    benchmarkInfo.Convert();
                    break;
                case StageEnum.Validate:
                    benchmarkInfo.Validate();
                    break;
                case StageEnum.DeserializeVars:
                    benchmarkInfo.DeserializeInputs();
                    break;
                case StageEnum.ParseVariables:
                    benchmarkInfo.ParseVariables();
                    break;
                case StageEnum.Execute:
                    benchmarkInfo.Execute();
                    break;
                case StageEnum.Serialize:
                    benchmarkInfo.Serialize();
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        //[Params(StageEnum.Build, StageEnum.TypicalExecution, StageEnum.Serialize)]
        [Params(StageEnum.Build, StageEnum.Parse, StageEnum.Convert, StageEnum.Validate, StageEnum.DeserializeVars, StageEnum.ParseVariables, StageEnum.Execute, StageEnum.Serialize)]
        public StageEnum Stage { get; set; }

        void IBenchmark.RunProfiler()
        {
            Stage = StageEnum.Build;
            Introspection();
        }

        public enum StageEnum
        {
            Build,
            Parse,
            Convert,
            Validate,
            DeserializeVars,
            ParseVariables,
            Execute,
            Serialize,
            TypicalExecution,
        }

        public class BenchmarkInfo
        {
            public ISchema Schema;
            public Func<ISchema> SchemaBuilder;
            public string Query;
            public string InputsString;
            public GraphQLDocument GraphQLDocument;
            public Document Document;
            public Operation Operation;
            public Inputs Inputs;
            public Language.AST.Variables Variables;
            public ExecutionResult ExecutionResult;

            public BenchmarkInfo(string query, string inputs, Func<ISchema> schemaBuilder)
            {
                // this exercises all the code in case of any errors, in addition to prep for each stage of testing
                SchemaBuilder = schemaBuilder;
                Schema = BuildSchema();
                Query = query;
                GraphQLDocument = Parse();
                Document = Convert();
                Operation = Document.Operations.FirstOrDefault();
                InputsString = inputs;
                Inputs = DeserializeInputs();
                Variables = ParseVariables();
                ExecutionResult = Execute();
                _ = Serialize();
            }

            public ISchema BuildSchema()
            {
                return SchemaBuilder();
            }

            public GraphQLDocument Parse()
            {
                return GraphQLParser.Parser.Parse(Query, new GraphQLParser.ParserOptions { Ignore = GraphQLParser.IgnoreOptions.IgnoreComments });
            }

            public Document Convert()
            {
                return CoreToVanillaConverter.Convert(GraphQLDocument);
            }

            public Inputs DeserializeInputs()
            {
                return InputsString?.ToInputs();
            }

            public Language.AST.Variables ParseVariables()
            {
                return Inputs == null ? null : new ValidationContext().GetVariableValues(Schema, Operation.Variables, Inputs);
            }

            private static readonly DocumentValidator _documentValidator = new DocumentValidator();
            public IValidationResult Validate()
            {
                return _documentValidator.ValidateAsync(
                    Schema,
                    Document,
                    null,
                    null,
                    Inputs).Result.validationResult;
            }

            private static readonly ParallelExecutionStrategy _parallelExecutionStrategy = new ParallelExecutionStrategy();
            public ExecutionResult Execute()
            {
                var context = new ExecutionContext
                {
                    Document = Document,
                    Schema = Schema,
                    RootValue = null,
                    UserContext = new Dictionary<string, object>(),

                    Operation = Operation,
                    Variables = Variables,
                    Fragments = Document.Fragments,
                    Errors = new ExecutionErrors(),
                    Extensions = new Dictionary<string, object>(),
                    CancellationToken = default,

                    Metrics = Instrumentation.Metrics.None,
                    Listeners = new List<IDocumentExecutionListener>(),
                    ThrowOnUnhandledException = true,
                    UnhandledExceptionDelegate = context => { },
                    MaxParallelExecutionCount = int.MaxValue,
                    RequestServices = null
                };
                return _parallelExecutionStrategy.ExecuteAsync(context).Result;
            }

            private static readonly DocumentWriter _documentWriter = new DocumentWriter();
            public System.IO.MemoryStream Serialize()
            {
                var mem = new System.IO.MemoryStream();
                _documentWriter.WriteAsync(mem, ExecutionResult).GetAwaiter().GetResult();
                mem.Position = 0;
                return mem;
            }
        }
    }
}
