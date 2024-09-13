using System.Collections;
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
        // Create the root query ObjectGraphType directly without using a class definition.
        var query = new ObjectGraphType();

        // Define the first field that modifies the variable.
        query.Field<StringGraphType>("modifyVar")
            .Resolve(context =>
            {
                // Modify the variable (assume it's named "inputVar").
                context.Variables.Find("inputVar")!.Value = "modifiedValue";
                return "Variable modified";
            });

        // Define the second field that retrieves the modified variable value.
        query.Field<StringGraphType>("retrieveVar")
            .Argument<StringGraphType>("varArg")
            .Resolve(context =>
            {
                // Fetch the argument (which is pulled from the variable).
                var varArg = context.GetArgument<string>("varArg");
                return varArg; // Return the value from the variable.
            });

        // Create the schema and assign the query type.
        var schema = new Schema { Query = query };

        // Define the query and the variables for execution.
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

        // Execute the GraphQL query.
        var result = await new DocumentExecuter().ExecuteAsync(options);

        // Convert the result to JSON
        var resultJson = new GraphQLSerializer().Serialize(result);

        // Assert the result.
        resultJson.ShouldBeCrossPlatJson("""
            {
                "data": {
                    "modifyVar": "Variable modified",
                    "retrieveVar": "modifiedValue"
                }
            }
            """);
    }

    // `FieldMappingValidationRule` is a custom validation rule that extends the base class `ValidationRuleBase`.
    // This rule will be used to validate GraphQL requests after they are parsed into an AST (Abstract Syntax Tree).
    public class FieldMappingValidationRule : ValidationRuleBase
    {
        // `GetPostNodeVisitorAsync` method returns a node visitor, which will visit each AST node in the parsed GraphQL document.
        // In this case, it creates and returns a new instance of `MyVisitor`.
        public override ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context) => new(new MyVisitor());

        // `MyVisitor` is an inner class that implements the `INodeVisitor` interface.
        // It is responsible for visiting specific nodes in the AST and performing logic on them.
        private class MyVisitor : INodeVisitor
        {
            // Dictionary that maps GraphQLField nodes to their corresponding FieldType objects.
            // This is used to store field definitions as the AST is traversed.
            private readonly Dictionary<GraphQLField, FieldType> _fields = new();

            // `EnterAsync` is called when entering a node in the AST. 
            // If the node is a `GraphQLField`, the corresponding `FieldType` is fetched and stored in the `_fields` dictionary.
            public ValueTask EnterAsync(ASTNode node, ValidationContext context)
            {
                // Check if the current AST node is a `GraphQLField`.
                if (node is GraphQLField field)
                {
                    // Retrieve the field definition (FieldType) from the validation context.
                    var fieldDef = context.TypeInfo.GetFieldDef();

                    // If the field definition is not null, store it in the `_fields` dictionary.
                    if (fieldDef != null)
                        _fields[field] = fieldDef;
                }
                return default;
            }

            // `LeaveAsync` is called when leaving a node in the AST. 
            // If the node is a `GraphQLDocument`, it adds the collected field mapping to the user context.
            public ValueTask LeaveAsync(ASTNode node, ValidationContext context)
            {
                // When the entire document is processed, the field mappings are stored in the `UserContext`.
                if (node is GraphQLDocument document)
                    context.UserContext["fieldMapping"] = _fields;
                return default;
            }
        }
    }

    // `DyanmicVariableExecutionListener` is a custom execution listener that is derived from `DocumentExecutionListenerBase`.
    // It modifies the operation so that argument values are retrieved at execution time rather than using coerced values during variable parsing.
    public class DynamicVariableExecutionListener : DocumentExecutionListenerBase
    {
        // `BeforeExecutionAsync` is invoked before the GraphQL execution begins.
        // It extracts the field mapping from the `UserContext` and creates a new dictionary (FieldArgumentDictionary) that maps GraphQL fields to argument values.
        public override Task BeforeExecutionAsync(IExecutionContext context)
        {
            // Cast the `IExecutionContext` to the internal `ExecutionContext` type.
            var context2 = (GraphQL.Execution.ExecutionContext)context;

            // Retrieve the field mapping from the `UserContext`.
            var fieldMapping = (Dictionary<GraphQLField, FieldType>)context.UserContext["fieldMapping"]!;

            // Create a new `FieldArgumentDictionary` object, which will map fields to their argument values.
            context2.ArgumentValues = new FieldArgumentDictionary(context, fieldMapping);
            return Task.CompletedTask;
        }

        // `FieldArgumentDictionary` is a custom dictionary-like class that implements `IReadOnlyDictionary`.
        // It maps `GraphQLField` objects to their associated argument values (`IDictionary<string, ArgumentValue>`).
        private class FieldArgumentDictionary : IReadOnlyDictionary<GraphQLField, IDictionary<string, ArgumentValue>>
        {
            private readonly IExecutionContext _executionContext;
            private readonly IDictionary<GraphQLField, FieldType> _fieldMappings;

            // These fields are used to cache the last field and its arguments to avoid recomputation.
            private GraphQLField? _lastField;
            private IDictionary<string, ArgumentValue>? _lastArgs;

            // Constructor that initializes the `FieldArgumentDictionary` with an execution context and a field mapping.
            public FieldArgumentDictionary(IExecutionContext executionContext, IDictionary<GraphQLField, FieldType> fieldMappings)
            {
                _executionContext = executionContext;
                _fieldMappings = fieldMappings;
            }

            // Indexer to access argument values for a given `GraphQLField`.
            public IDictionary<string, ArgumentValue> this[GraphQLField key]
            {
                get
                {
                    // If the field exists in the dictionary, return its argument values.
                    if (TryGetValue(key, out var value))
                        return value!;
                    throw new ArgumentOutOfRangeException(nameof(key));
                }
            }

            // Property stubs required by `IReadOnlyDictionary`, not implemented in this example.
            public IEnumerable<GraphQLField> Keys => throw new NotImplementedException();
            public IEnumerable<IDictionary<string, ArgumentValue>> Values => throw new NotImplementedException();
            public int Count => throw new NotImplementedException();

            // Checks if the `FieldArgumentDictionary` contains a mapping for the specified field.
            public bool ContainsKey(GraphQLField key) => _fieldMappings.ContainsKey(key);

            // Returns an enumerator, though it is not implemented here.
            public IEnumerator<KeyValuePair<GraphQLField, IDictionary<string, ArgumentValue>>> GetEnumerator() => throw new NotImplementedException();

            // `TryGetValue` is the core logic for retrieving argument values for a given field.
            public bool TryGetValue(GraphQLField key, out IDictionary<string, ArgumentValue> value)
            {
#pragma warning disable RCS1059 // Avoid locking on publicly accessible instance
                // Locking is used to ensure thread safety when caching field-argument mappings.
                lock (this)
                {
                    // If the last accessed field matches the current field, return cached arguments.
                    if (key == _lastField)
                    {
                        value = _lastArgs!;
                        return true;
                    }
                }

                // Fetch the field's type information from the mapping.
                var fieldType = _fieldMappings[key];

                // Retrieve the arguments for the field using the `ExecutionHelper`.
                // The `ExecutionHelper.GetArguments` method computes the arguments by using the field type and provided arguments in the request.
                IDictionary<string, ArgumentValue>? value2 = ExecutionHelper.GetArguments(
                    fieldType.Arguments,
                    key.Arguments,
                    _executionContext.Variables,
                    _executionContext.Document,
                    key,
                    null
                );

                // If no arguments are found, return an empty immutable dictionary.
                value = value2 ?? ImmutableDictionary<string, ArgumentValue>.Empty;

                // Cache the current field and its arguments to avoid recomputation.
                lock (this)
                {
                    _lastField = key;
                    _lastArgs = value;
                }

                return true;
#pragma warning restore RCS1059 // Avoid locking on publicly accessible instance
            }

            // Another enumerator method, also not implemented in this example.
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }
    }
}
