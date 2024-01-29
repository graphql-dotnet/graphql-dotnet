# Query Validation

There [are a number of query validation rules](https://spec.graphql.org/October2021/#sec-Validation)
that are ran when a query is executed. All of these are turned on by default. You can add custom validation
rules by calling `.AddValidationRule<T>()` within your DI configuration as follows:

```csharp
services.AddGraphQL(b => b
  .AddSchema<MySchema>()
  .AddSystemTextJson()
  .AddValidationRule<RequiresAuthValidationRule>());
```

When not using the DI builder methods, you can set the `ExecutionOptions.ValidationRules` property when
calling `IDocumentExecuter.ExecuteAsync` as follows:

```csharp
await schema.ExecuteAsync(_ =>
{
  _.Query = "...";
  _.ValidationRules =
    new[]
    {
      new RequiresAuthValidationRule()
    }
    .Concat(DocumentValidator.CoreRules);
});
```

## Setting validation rules on input arguments or input object fields

When defining a schema, you can set validation rules on input arguments or input object fields.
This can be used to easily validate input values such as email addresses, phone numbers, or to
validate a value is within a specific range. The `Validator` delegate is used to validate the
input value, and can be set via the `Validate` method on the `FieldBuilder` or `QueryArgument`.
Here are some examples:

```csharp
// for an input object graph type

Field(x => x.FirstName)
    .Validate(value =>
    {
        if (((string)value).Length >= 10)
            throw new ArgumentException("Length must be less than 10 characters.");
    });

Field(x => x.Age)
    .Validate(value =>
    {
        if ((int)value < 18)
            throw new ArgumentException("Age must be 18 or older.");
    });

Field(x => x.Password)
    .Validate(value =>
    {
        VerifyPasswordComplexity((string)value);
    });
```

The `Validator` delegate is called during the validation stage, prior to execution of the request.
Null values are not passed to the validation function. Supplying an invalid value will produce
a response similar to the following:

```json
{
  "errors": [
    {
      "message": "Invalid value for argument 'firstName' of field 'testMe'. Length must be less than 10 characters.",
      "locations": [
        {
          "line": 1,
          "column": 14
        }
      ],
      "extensions": {
        "code": "INVALID_VALUE",
        "codes": [
          "INVALID_VALUE",
          "ARGUMENT"
        ],
        "number": "5.6"
      }
    }
  ]
}
```

For type-first schemas, you may define your own attributes to perform validation, either on input
fields or on output field arguments. For example:

```csharp
// for AutoRegisteringObjectGraphType<MyClass>

public class MyClass
{
    public static string TestMe([MyMaxLength(5)] string value) => value;
}

private class MyMaxLength : GraphQLAttribute
{
    private readonly int _maxLength;
    public MyMaxLength(int maxLength)
    {
        _maxLength = maxLength;
    }

    public override void Modify(ArgumentInformation argumentInformation)
    {
        if (argumentInformation.TypeInformation.Type != typeof(string))
        {
            throw new InvalidOperationException("MyMaxLength can only be used on string arguments.");
        }
    }

    public override void Modify(QueryArgument queryArgument)
    {
        queryArgument.Validate(value =>
        {
            if (((string)value).Length > _maxLength)
            {
                throw new ArgumentException($"Value is too long. Max length is {_maxLength}.");
            }
        });
    }
}
```

When using the `Validator` delegate, there is no need write or install a custom validation rule
to handle the validation. The `Validator` delegate is called during the validation stage, and
will not unnecessarily trigger the unhandled exception handler due to client input errors.

At this time GraphQL.NET does not directly support the `MaxLength` and similar attributes from
`System.ComponentModel.DataAnnotations`, but this may be added in a future version. You can
implement your own attributes as shown above, or call the `Validate` method to set a validation
function.

## Custom Validation Rules

Validation rules are built with the `IValidationRule` interface and can validate the document and variables
at different stages of the validation process: before parsing arguments, during parsing of variables, and
after parsing arguments. The `INodeVisitor` interface is used to traverse the AST and report errors via
the `ValidationContext` class, while the `IVariableVisitor` interface is used to validate variables.
Also relevant is the `TypeInfo` class, which provides type information for the current position in the document,
and the `MatchingNodeVisitor` and `NodeVisitors` helper classes for creating and combining node visitors.

### `IValidationRule` interface

`IValidationRule` represents a validation rule for a GraphQL document. It consists of three methods:

1. `GetPreNodeVisitorAsync`: Returns a node visitor for validating the document before parsing arguments.
2. `GetVariableVisitorAsync`: Returns a visitor used while parsing variables.
3. `GetPostNodeVisitorAsync`: Returns a node visitor for validating the document after parsing all arguments.

### `INodeVisitor` interface

`INodeVisitor` handles events raised by a node walker. It has two methods:

1. `EnterAsync(ASTNode node, ValidationContext context)`: Called when entering a node.
2. `LeaveAsync(ASTNode node, ValidationContext context)`: Called when leaving a node.

### `IVariableVisitor` interface

`IVariableVisitor` is used to validate variables. It has four methods:

1. `VisitScalarAsync`: Called when parsing a scalar value.
2. `VisitListAsync`: Called when parsing a list value.
3. `VisitObjectAsync`: Called when parsing an input object value.
4. `VisitFieldAsync`: Called when parsing a field of an input object value.

This interface mainly exists for legacy compatibility and is not recommended for new code. Rather, use the
`Validate` method on `FieldBuilder` or `QueryArgument` to validate input values. See the schema node visitor
sample at the bottom of this document for a technique to apply validation rules across the entire schema.

For an example of a custom validation rule that implements `GetVariableVisitorAsync`, see the
[`InputFieldsAndArgumentsOfCorrectLength` class](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL/Validation/Rules.Custom/InputFieldsAndArgumentsOfCorrectLength.cs).

### `ValidationRuleBase` class

`ValidationRuleBase` is an abstract class that provides a default implementation of `IValidationRule`. It can be
extended to create custom validation rules.

### `ValidationContext` class

`ValidationContext` is used to report errors and track the state of the validation process.
Most of these mirror the same properties provided to the execution engine within `ExecutionOptions`.
Other properties include the following:

| Property | Description |
|----------|-------------|
| `Document` | The parsed document AST being validated. |
| `Operation` | The operation requested to be executed. |
| `TypeInfo` | The type information for the current position in the document. |
| `ArgumentValues` | Within `GetPostNodeVisitorAsync`, contains the parsed argument values for the document. |
| `DirectiveValues` | Within `GetPostNodeVisitorAsync`, contains the parsed directive values for the document. |

| Method | Description |
|--------|-------------|
| `ReportError` | Reports an error found during validation. |

### `MatchingNodeVisitor` class

`MatchingNodeVisitor` is a helper class that simplifies the process of creating node visitors.
The constructor takes a delegate that is called when a node matches the specified type.
The delegate can be synchronous or asynchronous, and a second delegate can be provided
to be called when leaving the node. See the examples below for usage.

### `NodeVisitors` class

`NodeVisitors` is a helper class that provides methods for combining node visitors.
The constructor accepts an array of node visitors and calls them in order when entering and leaving nodes.

### `TypeInfo` class

`TypeInfo` provides type information for the current node within the document. See xml comments
for exact details on each method.

| Method | Description |
|--------|-------------|
| `GetAncestor` | Returns the ancestor of the current node. |
| `GetLastType` | Returns the last graph type matched. |
| `GetInputType` | Returns the last input graph type matched. |
| `GetParentType` | Returns the parent type of the current node. |
| `GetFieldDef` | Returns the field definition for the current node. |
| `GetDirective` | Returns the last directive matched. |
| `GetArgument` | Returns the last argument matched. |

### Example 1: Disabling Introspection Requests

This rule prevents introspection requests.

```csharp
/// <summary>
/// Analyzes the document for any introspection fields and reports an error if any are found.
/// </summary>
public class NoIntrospectionValidationRule : ValidationRuleBase, INodeVisitor
{
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(this);

    ValueTask INodeVisitor.EnterAsync(ASTNode node, ValidationContext context)
    {
        if (node is GraphQLField field)
        {
            if (field.Name.Value == "__schema" || field.Name.Value == "__type")
                context.ReportError(new NoIntrospectionError(context.Document.Source, field));
        }
        return default;
    }

    ValueTask INodeVisitor.LeaveAsync(ASTNode node, ValidationContext context) => default;
}
```

Or when using the `MatchingNodeVisitor` helper class:

```csharp
public class NoIntrospectionValidationRule : ValidationRuleBase
{
    private static readonly MatchingNodeVisitor<GraphQLField> _visitor = new(
        (field, context) =>
        {
            if (field.Name.Value == "__schema" || field.Name.Value == "__type")
                context.ReportError(new NoIntrospectionError(context.Document.Source, field));
        });

    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_visitor);
}
```

### Example 2: Limiting Connections to Under 1000 Rows

This rule limits the number of rows returned in a connection to 1000.

```csharp
services.AddGraphQL(b => b
    .AddSchema<MySchema>()
    .AddValidationRule<NoConnectionOver1000ValidationRule>());

public class NoConnectionOver1000ValidationRule : ValidationRuleBase, IVariableVisitorProvider, INodeVisitor
{
    public override ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context)
        => context.ArgumentValues != null ? new(this) : default;

    ValueTask INodeVisitor.EnterAsync(ASTNode node, ValidationContext context)
    {
        if (node is not GraphQLField fieldNode)
            return default;

        var fieldDef = context.TypeInfo.GetFieldDef();
        if (fieldDef == null || fieldDef.ResolvedType?.GetNamedType() is not IObjectGraphType connectionType || !connectionType.Name.EndsWith("Connection"))
            return default;

        if (!(context.ArgumentValues?.TryGetValue(fieldNode, out var args) ?? false))
            return default;

        ArgumentValue lastArg = default;
        if (!args.TryGetValue("first", out var firstArg) && !args.TryGetValue("last", out lastArg))
            return default;

        var rows = (int?)firstArg.Value ?? (int?)lastArg.Value ?? 0;
        if (rows > 1000)
            context.ReportError(new ValidationError("Cannot return more than 1000 rows"));

        return default;
    }

    ValueTask INodeVisitor.LeaveAsync(ASTNode node, ValidationContext context) => default;
}
```

## Adding validation rules via schema node visitor

You may also add validation rules via a schema node visitor. The below sample performs the same
validation as the previous example, but uses a schema node visitor. The schema node visitor is
called when the schema is built, and adds the appropriate validation rules to the schema.

```csharp
services.AddGraphQL(b => b
    .AddSchema<MySchema>()
    .AddSchemaVisitor<NoConnectionOver1000Visitor>());
    
public class NoConnectionOver1000Visitor : BaseSchemaNodeVisitor
{
    public override void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema)
        => argument.Validator += GetValidator(argument, field);

    public override void VisitInterfaceFieldArgumentDefinition(QueryArgument argument, FieldType field, IInterfaceGraphType type, ISchema schema)
        => field.Validator += GetValidator(argument, field);

    private static Action<object?>? GetValidator(QueryArgument argument, FieldType field)
    {
        // identify fields that return a connection type
        if (!field.ResolvedType!.GetNamedType().Name.EndsWith("Connection"))
            return null;

        // identify the first and last arguments
        if (argument.Name != "first" && argument.Name != "last")
            return null;

        // apply the validation rule
        return value =>
        {
            if (value is int intValue && intValue > 1000)
                throw new ArgumentException("Cannot return more than 1000 rows.");
        };
    }
}
```

With the visitor approach, the validation is only evaluated at runtime when the applicable
field is requested, with no performance penalty otherwise. It is also considerably simpler
to implement.
