using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using GraphQL.Execution;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.Tests.Bugs;

public class Issue4060
{
    [Fact]
    public async Task VariableModification_ShouldReflectInSecondField()
    {
        var query = new ObjectGraphType();

        query.Field<StringGraphType>("modifyVar")
            .Resolve(context =>
            {
                context.Variables.Find("inputVar")!.Value = "modifiedValue";
                return "Variable modified";
            });

        query.Field<StringGraphType>("retrieveVar")
            .Argument<StringGraphType>("varArg")
            .Resolve(context => context.GetArgument<string>("varArg"));

        var schema = new Schema { Query = query };

        var options = new ExecutionOptions
        {
            Schema = schema,
            Query = """
                query ($inputVar: String = "originalValue") {
                    modifyVar
                    retrieveVar(varArg: $inputVar)
                }
                """,
            Variables = new Inputs(new Dictionary<string, object?>
                {
                    { "inputVar", "originalValue" }
                }),
            ValidationRules = DocumentValidator.CoreRules.Append(new FieldMappingValidationRule()),
        };
        options.Listeners.Add(new DynamicVariableExecutionListener());

        var result = await new DocumentExecuter().ExecuteAsync(options);
        var resultJson = new GraphQLSerializer().Serialize(result);

        resultJson.ShouldBeCrossPlatJson("""
            {
                "data": {
                    "modifyVar": "Variable modified",
                    "retrieveVar": "modifiedValue"
                }
            }
            """);
    }

    [Fact]
    public async Task SampleCodeWorksWhenMergingSelectionSets()
    {
        var heroType = new ObjectGraphType() { Name = "Hero" };
        heroType.Field<IntGraphType>("id");
        heroType.Field<StringGraphType>("name");
        var query = new ObjectGraphType() { Name = "Query" };
        query.Field("hero", heroType)
            .Argument<IntGraphType>("id")
            .Resolve(context => new { id = context.GetArgument<int>("id"), name = "John Doe" });
        var schema = new Schema { Query = query };
        schema.Initialize();
        var options = new ExecutionOptions
        {
            Schema = schema,
            Query = """
                query heroQuery($id: Int!) {
                    ...fragment1
                    ...fragment2
                }
                fragment fragment1 on Query {
                    hero(id: $id) {
                        id
                    }
                }
                fragment fragment2 on Query {
                    hero(id: $id) {
                        name
                    }
                }
                """,
            Variables = new Inputs(new Dictionary<string, object?>
                {
                    { "id", 123 }
                }),
            ValidationRules = DocumentValidator.CoreRules.Append(new FieldMappingValidationRule()),
        };
        options.Listeners.Add(new DynamicVariableExecutionListener());

        var result = await new DocumentExecuter().ExecuteAsync(options);
        var resultJson = new GraphQLSerializer().Serialize(result);

        resultJson.ShouldBeCrossPlatJson("""
            {
                "data": {
                    "hero": {
                        "id": 123,
                        "name": "John Doe"
                    }
                }
            }
            """);
    }

    public class FieldMappingValidationRule : ValidationRuleBase
    {
        public override ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context) => new(new MyVisitor());

        private class MyVisitor : INodeVisitor
        {
            private readonly Dictionary<GraphQLField, FieldType> _fields = [];

            public ValueTask EnterAsync(ASTNode node, ValidationContext context)
            {
                if (node is GraphQLField field)
                {
                    var fieldDef = context.TypeInfo.GetFieldDef();

                    if (fieldDef != null)
                        _fields[field] = fieldDef;
                }
                return default;
            }

            public ValueTask LeaveAsync(ASTNode node, ValidationContext context)
            {
                if (node is GraphQLDocument document)
                    context.UserContext["fieldMapping"] = _fields;
                return default;
            }
        }
    }

    public class DynamicVariableExecutionListener : DocumentExecutionListenerBase
    {
        public override Task BeforeExecutionAsync(IExecutionContext context)
        {
            var context2 = (GraphQL.Execution.ExecutionContext)context;
            var fieldMapping = (Dictionary<GraphQLField, FieldType>)context.UserContext["fieldMapping"]!;
            context2.ArgumentValues = new FieldArgumentDictionary(context, fieldMapping);
            return Task.CompletedTask;
        }

        private class FieldArgumentDictionary : IReadOnlyDictionary<GraphQLField, IDictionary<string, ArgumentValue>>, IDictionary<GraphQLField, IDictionary<string, ArgumentValue>>
        {
            private readonly IExecutionContext _executionContext;
            private readonly IDictionary<GraphQLField, FieldType> _fieldMappings;
            private GraphQLField? _lastField;
            private IDictionary<string, ArgumentValue>? _lastArgs;
            private readonly object _lastFieldLock = new();
            private ConcurrentDictionary<GraphQLField, IDictionary<string, ArgumentValue>>? _overrideDictionary;

            public FieldArgumentDictionary(IExecutionContext executionContext, IDictionary<GraphQLField, FieldType> fieldMappings)
            {
                _executionContext = executionContext;
                _fieldMappings = fieldMappings;
            }

            public IDictionary<string, ArgumentValue> this[GraphQLField key]
            {
                get
                {
                    if (TryGetValue(key, out var value))
                        return value;
                    throw new ArgumentOutOfRangeException(nameof(key));
                }
                set => (_overrideDictionary ??= new())[key] = value;
            }

            public IEnumerable<GraphQLField> Keys => throw new NotImplementedException();
            public IEnumerable<IDictionary<string, ArgumentValue>> Values => throw new NotImplementedException();
            public int Count => throw new NotImplementedException();

            ICollection<GraphQLField> IDictionary<GraphQLField, IDictionary<string, ArgumentValue>>.Keys => throw new NotImplementedException();

            ICollection<IDictionary<string, ArgumentValue>> IDictionary<GraphQLField, IDictionary<string, ArgumentValue>>.Values => throw new NotImplementedException();

            bool ICollection<KeyValuePair<GraphQLField, IDictionary<string, ArgumentValue>>>.IsReadOnly => throw new NotImplementedException();

            public bool ContainsKey(GraphQLField key) => _fieldMappings.ContainsKey(key);

            public IEnumerator<KeyValuePair<GraphQLField, IDictionary<string, ArgumentValue>>> GetEnumerator() => throw new NotImplementedException();

            public bool TryGetValue(GraphQLField key, out IDictionary<string, ArgumentValue> value)
            {
                if (_overrideDictionary?.TryGetValue(key, out value!) == true)
                    return true;

                lock (_lastFieldLock)
                {
                    if (key == _lastField)
                    {
                        value = _lastArgs!;
                        return true;
                    }
                }

                var fieldType = _fieldMappings[key];

                IDictionary<string, ArgumentValue>? value2 = ExecutionHelper.GetArguments(
                    fieldType.Arguments,
                    key.Arguments,
                    _executionContext.Variables,
                    _executionContext.Document,
                    key,
                    null
                );

                value = value2 ?? ImmutableDictionary<string, ArgumentValue>.Empty;

                lock (_lastFieldLock)
                {
                    _lastField = key;
                    _lastArgs = value;
                }

                return true;
            }

            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
            void IDictionary<GraphQLField, IDictionary<string, ArgumentValue>>.Add(GraphQLField key, IDictionary<string, ArgumentValue> value) => throw new NotImplementedException();
            bool IDictionary<GraphQLField, IDictionary<string, ArgumentValue>>.Remove(GraphQLField key) => throw new NotImplementedException();
            void ICollection<KeyValuePair<GraphQLField, IDictionary<string, ArgumentValue>>>.Add(KeyValuePair<GraphQLField, IDictionary<string, ArgumentValue>> item) => throw new NotImplementedException();
            void ICollection<KeyValuePair<GraphQLField, IDictionary<string, ArgumentValue>>>.Clear() => throw new NotImplementedException();
            bool ICollection<KeyValuePair<GraphQLField, IDictionary<string, ArgumentValue>>>.Contains(KeyValuePair<GraphQLField, IDictionary<string, ArgumentValue>> item) => throw new NotImplementedException();
            void ICollection<KeyValuePair<GraphQLField, IDictionary<string, ArgumentValue>>>.CopyTo(KeyValuePair<GraphQLField, IDictionary<string, ArgumentValue>>[] array, int arrayIndex) => throw new NotImplementedException();
            bool ICollection<KeyValuePair<GraphQLField, IDictionary<string, ArgumentValue>>>.Remove(KeyValuePair<GraphQLField, IDictionary<string, ArgumentValue>> item) => throw new NotImplementedException();
        }
    }
}
