# Custom Validation Rules in GraphQL.NET v8

This guide explains how to create custom validation rules for GraphQL.NET v8, covering the validation pipeline, core interfaces, field-level validation methods (Parser/Validator), and complete working examples.

## Table of Contents

- [Overview](#overview)
- [Built-in Validation Rules](#built-in-validation-rules)
- [The Validation Pipeline](#the-validation-pipeline)
- [Core Interfaces](#core-interfaces)
- [Creating Custom Validation Rules](#creating-custom-validation-rules)
  - [Pattern 1: Pre-Node Visitor (Access Control)](#pattern-1-pre-node-visitor-access-control)
  - [Pattern 2: Stateful Visitor (Query Depth Limit)](#pattern-2-stateful-visitor-query-depth-limit)
  - [Pattern 3: Post-Node Visitor (Argument Validation)](#pattern-3-post-node-visitor-argument-validation)
  - [Pattern 4: Variable Visitor](#pattern-4-variable-visitor)
- [Field-Level Validation](#field-level-validation)
  - [Parser and Validator on FieldType](#parser-and-validator-on-fieldtype)
  - [ValidateArguments on FieldType](#validatearguments-on-fieldtype)
- [Attribute-Based Validation](#attribute-based-validation)
  - [ParserAttribute](#parserattribute)
  - [ValidatorAttribute](#validatorattribute)
  - [ValidateArgumentsAttribute](#validateargumentsattribute)
- [Registering Custom Rules](#registering-custom-rules)
- [Cached Document Validation Rules](#cached-document-validation-rules)
- [Complete Examples](#complete-examples)
- [Best Practices](#best-practices)

---

## Overview

Validation in GraphQL.NET ensures that incoming queries are well-formed, safe, and comply with your application's rules before execution. The validation system is built on a visitor pattern that walks the GraphQL AST (Abstract Syntax Tree) and allows rules to inspect or reject parts of the query.

Key concepts:
- **Validation Rules** implement `IValidationRule` (typically by extending `ValidationRuleBase`)
- **Node Visitors** are called as the AST is traversed — enter/leave each node
- **Variable Visitors** are called during variable parsing
- The validation pipeline runs in three phases: **Pre-node → Variable parsing → Post-node**

---

## Built-in Validation Rules

GraphQL.NET includes a comprehensive set of built-in rules (the "Core Rules") that enforce the GraphQL specification:

| Rule | Description |
|------|-------------|
| `UniqueOperationNames` | Ensures each operation has a unique name |
| `LoneAnonymousOperation` | Anonymous operations must be the only operation |
| `SingleRootFieldSubscriptions` | Subscriptions can only have one root field |
| `KnownTypeNames` | All referenced types must exist in the schema |
| `FragmentsOnCompositeTypes` | Fragments can only be on composite types |
| `VariablesAreInputTypes` | Variables must be input types |
| `ScalarLeafs` | Scalar fields must not have sub-selections |
| `FieldsOnCorrectType` | Fields must exist on their parent type |
| `UniqueFragmentNames` | Fragment names must be unique |
| `KnownFragmentNames` | Referenced fragments must exist |
| `NoUnusedFragments` | All defined fragments must be used |
| `PossibleFragmentSpreads` | Fragment spreads must be possible |
| `NoFragmentCycles` | Fragment spreads must not form cycles |
| `NoUndefinedVariables` | All used variables must be defined |
| `NoUnusedVariables` | All defined variables must be used |
| `UniqueVariableNames` | Variable names must be unique within an operation |
| `KnownDirectivesInAllowedLocations` | Directives must be known and in valid locations |
| `UniqueDirectivesPerLocation` | Non-repeatable directives must be unique per location |
| `KnownArgumentNames` | All arguments must be defined on their field/directive |
| `UniqueArgumentNames` | Argument names must be unique per field/directive |
| `ArgumentsOfCorrectType` | Argument literals must be of the correct type |
| `ProvidedNonNullArguments` | Required arguments must be provided |
| `DefaultValuesOfCorrectType` | Variable default values must match their type |
| `VariablesInAllowedPosition` | Variables must be usable where they are referenced |
| `UniqueInputFieldNames` | Input object fields must have unique names |
| `OverlappingFieldsCanBeMerged` | Fields that can be merged must not conflict |
| `FieldArgumentsAreValidRule` | Runs field-level `ValidateArguments` callbacks |

Additionally, custom rules shipped with the library include:

| Rule | Description |
|------|-------------|
| `ComplexityValidationRule` | Analyzes query complexity and depth against configurable thresholds |
| `NoIntrospectionValidationRule` | Blocks introspection queries (security hardening) |
| `InputFieldsAndArgumentsOfCorrectLength` | Validates string length via `@length` directive |
| `DeprecatedElementsValidationRule` | Abstract rule for handling deprecated field/type usage |

---

## The Validation Pipeline

The `DocumentValidator` processes validation in three phases:

```
┌─────────────────────────────────┐
│  Phase 1: Pre-Node Visitors     │  ← GetPreNodeVisitorAsync()
│  (AST traversal before parsing) │
│  TypeInfo tracks current type   │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  Phase 2: Variable Parsing      │  ← GetVariableVisitorAsync()
│  (Parse and validate variables) │
│  IVariableVisitor callbacks     │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  Phase 3: Post-Node Visitors    │  ← GetPostNodeVisitorAsync()
│  (AST traversal after parsing)  │
│  ArgumentValues available       │
└─────────────────────────────────┘
```

- **Phase 1** is ideal for structural checks and access control (no parsed values needed)
- **Phase 2** is for validating variable values during parsing
- **Phase 3** is for validations that need access to parsed argument/directive values

---

## Core Interfaces

### IValidationRule

```csharp
public interface IValidationRule
{
    // Phase 1: Called before arguments are parsed
    ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context);

    // Phase 2: Called during variable parsing
    ValueTask<IVariableVisitor?> GetVariableVisitorAsync(ValidationContext context);

    // Phase 3: Called after all arguments are parsed
    ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context);
}
```

### ValidationRuleBase

The base class with virtual methods that return `default` (no visitor). Override only what you need:

```csharp
public abstract class ValidationRuleBase : IValidationRule
{
    public virtual ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => default;
    public virtual ValueTask<IVariableVisitor?> GetVariableVisitorAsync(ValidationContext context) => default;
    public virtual ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context) => default;
}
```

### ValidationContext

Provides all the context a validation rule needs:

| Property | Description |
|----------|-------------|
| `Schema` | The GraphQL schema |
| `Document` | The parsed GraphQL AST document |
| `Operation` | The operation to execute |
| `TypeInfo` | Tracks current type/field during traversal |
| `User` | `ClaimsPrincipal` from the request |
| `Variables` | Raw input variables |
| `UserContext` | Custom user context dictionary |
| `ArgumentValues` | Parsed field arguments (Phase 3 only) |
| `DirectiveValues` | Parsed directive arguments (Phase 3 only) |
| `Errors` | Collected validation errors |

### INodeVisitor

```csharp
public interface INodeVisitor
{
    ValueTask EnterAsync(ASTNode node, ValidationContext context);
    ValueTask LeaveAsync(ASTNode node, ValidationContext context);
}
```

### MatchingNodeVisitor&lt;TNode&gt;

A convenience class that only triggers for specific AST node types:

```csharp
var visitor = new MatchingNodeVisitor<GraphQLField>(
    enter: (fieldNode, context) =>
    {
        // Called for each field in the query
    },
    leave: (fieldNode, context) =>
    {
        // Called when leaving each field
    });
```

### TypeInfo Helper Methods

During node traversal, `TypeInfo` tracks the current position in the schema:

| Method | Returns |
|--------|---------|
| `GetFieldDef()` | Current `FieldType` |
| `GetParentType()` | Parent `IGraphType` |
| `GetLastType()` | Current type being visited |
| `GetArgument()` | Current argument definition |
| `GetInputType(int count)` | Input type at the specified stack depth |

---

## Creating Custom Validation Rules

### Pattern 1: Pre-Node Visitor (Access Control)

Pre-node visitors run **before** arguments are parsed. Ideal for access control checks.

```csharp
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

/// <summary>
/// Restricts fields marked with "RequiresAuth" metadata to authenticated users.
/// </summary>
public class RequiresAuthenticationRule : ValidationRuleBase
{
    public const string REQUIRES_AUTH_KEY = "RequiresAuth";

    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(_visitor);

    private static readonly MatchingNodeVisitor<GraphQLField> _visitor = new(
        enter: (fieldNode, context) =>
        {
            var fieldDef = context.TypeInfo.GetFieldDef();
            if (fieldDef?.GetMetadata<bool>(REQUIRES_AUTH_KEY) == true)
            {
                if (context.User?.Identity?.IsAuthenticated != true)
                {
                    context.ReportError(new ValidationError(
                        context.Document.Source,
                        "AUTH_REQUIRED",
                        $"Field '{fieldDef.Name}' requires authentication.",
                        fieldNode));
                }
            }
        });
}
```

**Schema setup:**
```csharp
queryType.Field<StringGraphType>("secretData")
    .WithMetadata(RequiresAuthenticationRule.REQUIRES_AUTH_KEY, true)
    .Resolve(_ => "Secret!");
```

### Pattern 2: Stateful Visitor (Query Depth Limit)

For rules that need to track state across nodes, implement `INodeVisitor` directly:

```csharp
public class MaxQueryDepthRule : ValidationRuleBase
{
    private readonly int _maxDepth;

    public MaxQueryDepthRule(int maxDepth) => _maxDepth = maxDepth;

    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(new DepthVisitor(_maxDepth));

    private class DepthVisitor : INodeVisitor
    {
        private readonly int _maxDepth;
        private int _currentDepth;

        public DepthVisitor(int maxDepth) => _maxDepth = maxDepth;

        public ValueTask EnterAsync(ASTNode node, ValidationContext context)
        {
            if (node is GraphQLField)
            {
                _currentDepth++;
                if (_currentDepth > _maxDepth)
                {
                    context.ReportError(new ValidationError(
                        context.Document.Source,
                        "MAX_DEPTH",
                        $"Query depth {_currentDepth} exceeds maximum {_maxDepth}.",
                        node));
                }
            }
            return default;
        }

        public ValueTask LeaveAsync(ASTNode node, ValidationContext context)
        {
            if (node is GraphQLField)
                _currentDepth--;
            return default;
        }
    }
}
```

### Pattern 3: Post-Node Visitor (Argument Validation)

Post-node visitors run **after** arguments are parsed. Access parsed values via `context.ArgumentValues`:

```csharp
public class ArgumentRangeRule : ValidationRuleBase
{
    public override ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context)
        => new(_visitor);

    private static readonly MatchingNodeVisitor<GraphQLField> _visitor = new(
        enter: (fieldNode, context) =>
        {
            // ArgumentValues is available in post-node phase
            if (context.ArgumentValues?.TryGetValue(fieldNode, out var args) == true)
            {
                if (args.TryGetValue("limit", out var limit) && (int)limit.Value! > 1000)
                {
                    context.ReportError(new ValidationError(
                        context.Document.Source,
                        "LIMIT_EXCEEDED",
                        "Limit must not exceed 1000.",
                        fieldNode));
                }
            }
        });
}
```

### Pattern 4: Variable Visitor

Variable visitors are called during variable parsing. They can validate and transform values:

```csharp
public class StringValidationRule : ValidationRuleBase
{
    public override ValueTask<IVariableVisitor?> GetVariableVisitorAsync(ValidationContext context)
        => new(MyVisitor.Instance);

    private class MyVisitor : BaseVariableVisitor
    {
        public static readonly MyVisitor Instance = new();

        public override ValueTask VisitFieldAsync(
            ValidationContext context,
            GraphQLVariableDefinition variable,
            VariableName variableName,
            IInputObjectGraphType type,
            FieldType field,
            object? variableValue,
            object? parsedValue)
        {
            // Validate individual fields within input objects
            if (parsedValue is string str && str.Length > 1000)
            {
                context.ReportError(new ValidationError(
                    context.Document.Source,
                    "STRING_TOO_LONG",
                    $"Field '{field.Name}' exceeds maximum length of 1000.",
                    variable));
            }
            return default;
        }
    }
}
```

---

## Field-Level Validation

### Parser and Validator on FieldType

`FieldType` has two properties for validating input object fields:

- **`Parser`** (`Func<object, object>?`) — Transforms the value during parsing. Runs before `Validator`.
- **`Validator`** (`Action<object>?`) — Validates the parsed value. Throw an exception to report an error.

These apply to fields of **input object types** and are called during variable parsing.

**Using the fluent builder:**
```csharp
var inputType = new InputObjectGraphType<MyInput>();
inputType.Field(x => x.Email)
    .ParseValue(value => ((string)value).Trim().ToLowerInvariant())  // Transform
    .Validate(value =>
    {
        var email = (string)value;
        if (!email.Contains('@'))
            throw new ArgumentException("Invalid email format.");
    });
```

**Directly on FieldType:**
```csharp
var field = new FieldType
{
    Name = "email",
    Type = typeof(StringGraphType),
    Parser = value => ((string)value).Trim().ToLowerInvariant(),
    Validator = value =>
    {
        if (!((string)value).Contains('@'))
            throw new ArgumentException("Invalid email format.");
    }
};
```

### ValidateArguments on FieldType

`ValidateArguments` (`Func<FieldArgumentsValidationContext, ValueTask>?`) validates **output type field arguments** after they are parsed. This runs via the built-in `FieldArgumentsAreValidRule`.

**Using the fluent builder:**
```csharp
queryType.Field<StringGraphType>("search")
    .Argument<IntGraphType>("limit")
    .ValidateArguments(ctx =>
    {
        var limit = ctx.GetArgument<int>("limit");
        if (limit > 100)
            ctx.ReportError(new ValidationError(
                ctx.ValidationContext.Document.Source,
                "LIMIT_TOO_HIGH",
                "Limit must not exceed 100."));
    })
    .Resolve(ctx => $"Searching with limit {ctx.GetArgument<int>("limit")}");
```

The `FieldArgumentsValidationContext` struct provides:
- `FieldAst` — The AST node of the field
- `FieldDefinition` — The schema field definition
- `ValidationContext` — The full validation context
- `Arguments` — Parsed argument values
- `GetArgument<T>(name)` — Get a typed argument value
- `SetArgument(name, value)` — Modify an argument value
- `ReportError(error)` — Report a validation error

---

## Attribute-Based Validation

GraphQL.NET provides attributes for configuring validation declaratively on your .NET types.

### ParserAttribute

Applied to properties/fields of input classes. Points to a static method with signature `object MethodName(object value)`:

```csharp
public class MyInput
{
    [Parser("NormalizeEmail")]
    public string Email { get; set; }

    private static object NormalizeEmail(object value)
        => ((string)value).Trim().ToLowerInvariant();
}
```

Or reference a method on another type:

```csharp
[Parser(typeof(EmailHelper), "Normalize")]
public string Email { get; set; }
```

### ValidatorAttribute

Applied to properties/fields of input classes. Points to a static method with signature `void MethodName(object value)`. Can be applied multiple times:

```csharp
public class MyInput
{
    [Validator("ValidateEmail")]
    public string Email { get; set; }

    [Validator(typeof(StringValidators), "NotEmpty")]
    [Validator(typeof(StringValidators), "MaxLength50")]
    public string Name { get; set; }

    private static void ValidateEmail(object value)
    {
        if (!((string)value).Contains('@'))
            throw new ArgumentException("Invalid email format.");
    }
}
```

### ValidateArgumentsAttribute

Applied to methods in your graph type class. Points to a static method with signature `ValueTask MethodName(FieldArgumentsValidationContext context)`:

```csharp
public class MyQuery : ObjectGraphType
{
    [ValidateArguments("ValidateSearchArgs")]
    public string Search(IResolveFieldContext context)
    {
        return $"Searching for: {context.GetArgument<string>("query")}";
    }

    private static ValueTask ValidateSearchArgs(FieldArgumentsValidationContext ctx)
    {
        var query = ctx.GetArgument<string>("query");
        if (string.IsNullOrWhiteSpace(query))
            ctx.ReportError(new ValidationError(
                ctx.ValidationContext.Document.Source,
                "EMPTY_QUERY",
                "Search query must not be empty."));
        return default;
    }
}
```

Or reference a separate validator class:

```csharp
[ValidateArguments(typeof(SearchValidator))]
public string Search(IResolveFieldContext context) => ...;

public static class SearchValidator
{
    public static ValueTask ValidateArguments(FieldArgumentsValidationContext ctx)
    {
        // validation logic
        return default;
    }
}
```

---

## Registering Custom Rules

### Per-Request (ExecutionOptions)

Add rules to individual requests:

```csharp
var result = await executer.ExecuteAsync(options =>
{
    options.Schema = schema;
    options.Query = query;
    options.User = currentUser;

    // Append custom rules alongside core rules
    options.ValidationRules = DocumentValidator.CoreRules
        .Append(new RequiresAuthenticationRule())
        .Append(new MaxQueryDepthRule(10));
});
```

### Dependency Injection (Service Registration)

Register rules globally via DI:

```csharp
// In your startup/program.cs
builder.Services.AddGraphQL(b => b
    .AddSchema<MySchema>()
    .AddValidationRule<RequiresAuthenticationRule>()     // Per-request rule
    .AddValidationRule(() => new MaxQueryDepthRule(10))  // Factory method
);
```

Per-request rules are instantiated for each request. For cached document validation, use `AddCachedDocumentValidationRule`:

```csharp
builder.Services.AddGraphQL(b => b
    .AddSchema<MySchema>()
    .AddValidationRule<RequiresAuthenticationRule>()
    .AddCachedDocumentValidationRule<RequiresAuthenticationRule>()
);
```

### Replace Core Rules Entirely

You can replace all core rules:

```csharp
options.ValidationRules = new IValidationRule[]
{
    new RequiresAuthenticationRule(),
    new MaxQueryDepthRule(5),
    // Note: no core rules — only your rules apply
};
```

---

## Cached Document Validation Rules

When using document caching (e.g., `MemoryDocumentCache`), cached documents skip the initial validation. To run rules on cached documents too (e.g., authentication checks), use `CachedDocumentValidationRules`:

```csharp
var options = new ExecutionOptions
{
    Schema = schema,
    Query = query,
    CachedDocumentValidationRules = new IValidationRule[]
    {
        new RequiresAuthenticationRule(),
    },
};
```

**Important:** Rules that depend on per-request data (user identity, variables) should be registered as cached document validation rules. Rules that only check document structure do not need this.

---

## Complete Examples

See the accompanying sample project for complete, runnable code:

| File | Pattern |
|------|---------|
| `Rules/RequiresAuthenticationRule.cs` | Pre-node visitor for field-level auth |
| `Rules/RoleBasedAccessRule.cs` | Pre-node visitor with claims-based roles |
| `Rules/MaxQueryDepthRule.cs` | Stateful pre-node visitor for depth limiting |
| `Rules/MaxFieldCountRule.cs` | Post-node visitor for field count limiting |
| `Rules/ArgumentAndVariableValidationRules.cs` | Post-node visitor + variable visitor |
| `Program.cs` | Integration examples with all patterns |

---

## Best Practices

1. **Choose the right phase**: Use pre-node visitors for structural/access checks, variable visitors for value validation during parsing, and post-node visitors when you need parsed argument values.

2. **Reuse visitor instances**: For stateless rules, create the visitor as a `static readonly` field to avoid allocations.

3. **Use field metadata for configuration**: Instead of hardcoding field names, use `FieldType.WithMetadata()` and `GetMetadata<T>()` to make rules configurable per-field.

4. **Report errors early**: Return `default` from your visitor as soon as possible to avoid unnecessary processing.

5. **Use cached document rules for per-request validation**: Authentication and authorization rules should be registered as `CachedDocumentValidationRules` since they need to run on every request, even cached ones.

6. **Prefer built-in features when available**: Use `FieldType.ValidateArguments` with the `[ValidateArguments]` attribute for argument validation before creating a custom rule. Use the built-in `ComplexityValidationRule` for depth/complexity limiting.

7. **Keep rules focused**: Each rule should do one thing. Compose multiple rules rather than building monolithic rules.

8. **Handle null checks**: Always check for `null` returns from `TypeInfo.GetFieldDef()` and other methods, as fragments or type conditions may reference types that don't match.

---

## References

- [GraphQL Specification - Validation](https://spec.graphql.org/October2021/#sec-Validation)
- [GraphQL.NET Documentation](https://graphql-dotnet.github.io/docs/getting-started/introduction)
- [Sample Code in this Repository](./CustomValidationRules/)
