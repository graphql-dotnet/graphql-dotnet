# Migrating from v7.x to v8.x

:warning: For the best upgrade experience, please upgrade to 7.9 and use the included analyzers
to apply automatic code fixes to obsolete code patterns before upgrading to 8.0. :warning:

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A8.0+is%3Aissue+is%3Aclosed) and
[pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A8.0+is%3Aclosed) done in v8.

## Overview

GraphQL.NET v8 is a major release that includes many new features, including:

- Argument parsing now occurs during validation
- Fields and arguments can have custom parsers applied, which will catch more coercion errors during validation rather than execution
- Fields can configure validation code for argument values which will run during the validation stage
- Validation rules can execute after arguments have been parsed
- Complexity analyzer rewritten; can examine arguments to facilitate analysis
- Federation support is simplified and Federation 2 is supported
- Federation is supported for schema-first, code-first and type-first
- Input object coercion via `ToObject` is dynamically compiled
- `ComplexScalarGraphType` added, allowing flexible input/output types
- `IMetadataWriter` and similar interfaces added to allow better intellisense
- List converter enhancements and AOT support
- OneOf Input Object support (based on draft spec)
- Infer nullability by default for `Field` methods
- Add Persisted Document support (based on draft spec)
- Better error handling of GraphQL.NET pipeline; add timeout support
- Optimize scalar lists (e.g. `ListGraphType<IntGraphType>`)
- Allow GraphQL interfaces to implement other GraphQL interfaces (based on spec)

Some of these features require changes to the infrastructure, which can cause breaking changes during upgrades.
Most notably, if your server uses any of the following features, you are likely to encounter migration issues:

- Custom validation rules, schema node visitors, or IResolveFieldContext implementations
- The complexity analyzer
- Apollo Federation subgraph support
- Custom implementations of GraphQL.NET infrastructure (e.g. custom `IGraphType` implementations not based on an included class)
- Manual registration of GraphQL.NET infrastructure types (vs. using `AddGraphQL` builder methods)
- Input object graph types having models utilizing private constructors or non-default init-only properties
- Generic graph types
- Methods previously marked as obsolete
- GraphQL types which improperly implement interfaces
- Calls to `ExecutionHelper.GetArguments`

Below we have documented each new feature and breaking change, outlining the steps you need to take to upgrade your
application to v8.0. When possible, we have provided code examples to help you understand the changes required, and
options to revert behavior to v7.x if necessary.

Please keep in mind that methods and classes marked as obsolete in v8.x may be removed in v9.0. Each obsolete member
will include a message indicating the expected removal version.

For best results through the migration, we recommend following these steps:

1. Upgrade to GraphQL.NET v7.9
2. Use the included analyzers to apply automatic code fixes to obsolete code patterns
3. Implement a test that verifies the SDL of your schema matches the expected schema, as this will catch any changes
   the generated schema between versions; a sample of this code is provided at the bottom of this document
4. Set `GlobalSwitches.UseLegacyTypeNaming` to `false` and verify your type names have not changed (or override them),
   or set `GlobalSwitches.UseLegacyTypeNaming` to `true` to maintain v7.x behavior during the migration
5. Set `GlobalSwitches.InferFieldNullabilityFromNRTAnnotations` to `true` and verify your inferred field types
   have not changed, or set `GlobalSwitches.InferFieldNullabilityFromNRTAnnotations` to `false` to maintain v7.x behavior
6. Upgrade to GraphQL.NET v8.0

## New Features

### 1. `IMetadataReader`, `IMetadataWriter` and `IFieldMetadataWriter` interfaces added

This makes it convenient to add extension methods to graph types or fields that can be used to read or write metadata
such as authentication information. Methods for `IMetadataWriter` types will appear on both field builders and graph/field
types, while methods for `IMetadataReader` types will only appear on graph and field types. You can also access the
`IMetadataReader` reader from the `IMetadataWriter.MetadataReader` property. Here is an example:

```csharp
public static TMetadataBuilder RequireAdmin<TMetadataBuilder>(this TMetadataBuilder builder)
    where TMetadataBuilder : IMetadataWriter
{
    if (builder.MetadataReader.GetRoles?.Contains("Guests"))
        throw new InvalidOperationException("Cannot require admin and guest access at the same time.");
    return builder.AuthorizeWithRoles("Administrators");
}
```

Both interfaces extend `IProvideMetadata` with read/write access to the metadata contained within the graph or field type.
Be sure not to write metadata during the execution of a query, as the same graph/field type instance may be used for
multiple queries and you would run into concurrency issues.

In addition, the `IFieldMetadataWriter` interface has been added to allow scoping extension methods to fields only.
For example:

```csharp
// adds the GraphQL Federation '@requires' directive to the field
public static TMetadataWriter Requires<TMetadataWriter>(this TMetadataWriter fieldType, string fields)
    where TMetadataWriter : IFieldMetadataWriter
    => fieldType.ApplyDirective(PROVIDES_DIRECTIVE, d => d.AddArgument(new(FIELDS_ARGUMENT) { Value = fields }));
```

### 2. Built-in scalars may be overridden via DI registrations

For GraphQL.NET built-in scalars (such as `IntGraphType` or `GuidGraphType`), a dervied class may be registered
within the DI engine to facilitate replacement of the graph type throughout the schema versus calling `RegisterType`.

```csharp
services.AddSingleton<BooleanGraphType, MyBooleanGraphType>();
```

See https://graphql-dotnet.github.io/docs/getting-started/custom-scalars/#3-register-the-custom-scalar-within-your-schema
for more details.

### 3. Added `ComplexScalarGraphType`

This new scalar can be used to send or receive arbitrary objects or values to or from the server. It is functionally
equivalent to the `AnyGraphType` used for GraphQL Federation, but defaults to the name of `Complex` rather than `_Any`.

### 4. `Parser` delegates added to input field and argument definitions

This allows for custom parsing of input values. The `Parser` delegate is used to convert the input value
to the expected type, and can be set via the `ParseValue` method on the `FieldBuilder` or `QueryArgument`.

The auto-registering graph types will automatically configure the `Parser` delegate appropriately, and the
`Field(x => x.Property)` and `Field("FieldName", x => x.Property)` syntax will as well.

The most common use case for this is when using the ID graph type (passed via a string) for a numeric or GUID identifier.
For example, consider the following code:

```csharp
// for input object graph type
Field("Id", x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));

class MyInputObject
{
    public int Id { get; set; }
}
```

This will now cause an error when the client sends a string value for the Id field that cannot be coerced to
an `int` during the validation stage, rather than during the execution stage. Supplying an invalid value will
produce a response similar to the following:

```json
{
  "errors": [
    {
      "message": "Invalid value for argument 'id' of field 'testMe'. The input string 'abc' was not in a correct format.",
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
          "FORMAT"
        ],
        "number": "5.6"
      }
    }
  ]
}
```

This now is a validation error and not passed to the unhandled exception handler. Previously, this would have been
considered a server exception and processed by the unhandled exception handler, returning an error similar to the
following:

```json
{
  "errors": [
    {
      "message": "Error trying to resolve field 'testMe'.",
      "locations": [
        {
          "line": 1,
          "column": 3
        }
      ],
      "path": [
        "testMe"
      ],
      "extensions": {
        "code": "FORMAT",
        "codes": [
          "FORMAT"
        ]
      }
    }
  ],
  "data": null
}
```

You can also define a custom parser when appropriate to convert an input value to the expected type.
This is typically unnecessary when using the `Field(x => x.Property)` syntax, but when matching via
property name, it may be desired to define a custom parser. For example:

```csharp
// for input object graph type
Field<StringGraphType>("website") // match by property name, perhaps for a constructor argument
    .ParseValue(value => new Uri((string)value));

class MyInputObject
{
    public Uri? Website { get; set; }
}
```

Without adding a parser the coercion will occur within the resolver during `GetArgument<Uri>("abc")`
as occured in previous versions of GraphQL.NET. This will result in a server exception being thrown
and processed by the unhandled exception handler if the value cannot be coerced to a `Uri`. Note that
the parser function need not check for null values.

For type-first schemas in v8.1 and later, you may use the `[Parser]` attribute to define a custom parser
as shown in the below example:

```csharp
// sample for argument parsing
public class OutputClass1
{
    // use local private static method
    public static string Hello1([Parser(nameof(ParseHelloArgument))] string value) => value;

    // use public static method from another class -- looks for ParserClass.Parse
    public static string Hello2([Parser(typeof(ParserClass))] string value) => value;

    // use public static method from another class with a specific name
    public static string Hello3([Parser(typeof(HelperClass), nameof(HelperClass.ParseHelloArgument))] string value) => value;

    // example custom parser
    private static object ParseHelloArgument(object value) => (string)value + "test1";
}

// sample for input field parsing
public class InputClass1
{
    [Parser(nameof(ParseHelloArgument))]
    public string? Field1 { get; set; }

    private static object ParseHelloArgument(object value) => (string)value + "test1";
}
```

Note that this will replace the default parser configured by the auto-registering graph type.

### 5. `Validator` delegates added to input field and argument definitions

This allows for custom validation of input values. It can be used to easily validate input values
such as email addresses, phone numbers, or to validate a value is within a specific range. The
`Validator` delegate is used to validate the input value, and can be set via the `Validate` method
on the `FieldBuilder` or `QueryArgument`. Here are some examples:

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

In version 8.1 and newer, you can use the `[Validator]` attribute to define a custom validator
as shown in the below example:

```csharp
// sample for argument validation
public class OutputClass2
{
    // use local private static method
    public static string Hello1([Validator(nameof(ValidateHelloArgument))] string value) => value;

    // use public static method from another class -- looks for ValidatorClass.Validate
    public static string Hello2([Validator(typeof(ValidatorClass))] string value) => value;

    // use public static method from another class with a specific name
    public static string Hello3([Validator(typeof(HelperClass), nameof(HelperClass.ValidateHelloArgument))] string value) => value;

    // example custom validator
    private static void ValidateHelloArgument(object value)
    {
        if ((string)value != "hello")
            throw new ArgumentException("Value must be 'hello'.");
    }
}

// sample for input field validation
public class InputClass2
{
    [Validator(nameof(ValidateHelloArgument))]
    public string? Field1 { get; set; }

    private static void ValidateHelloArgument(object value)
    {
        if ((string)value != "hello")
            throw new ArgumentException("Value must be 'hello'.");
    }
}
```

Similar to the `Parser` delegate, the `Validator` delegate is called during the validation stage,
and will not unnecessarily trigger the unhandled exception handler due to client input errors.

At this time GraphQL.NET does not directly support the `MaxLength` and similar attributes from
`System.ComponentModel.DataAnnotations`, but this may be added in a future version. You can
implement your own attributes as shown above, or call the `Validate` method to set a validation
function.

### 6. `ArgumentValidator` delegate added to input object/interface field definitions (since 8.1)

This allows for custom validation of the coerced argument values. It can be used when you must
enforce certain relationships between arguments, such as ensuring that at least one of multiple
arguments are provided, or that a specific argument is provided when another argument is set to
a specific value. It can be set by configuring the `ValidateArguments` delegate on the `FieldType`
class or calling the `ValidateArguments` method on the field builder. Here is an example:

```csharp
Field<string>("example")
    .Argument<string>("str1", true)
    .Argument<string>("str2", true)
    .ValidateArguments(ctx =>
    {
        var str1 = ctx.GetArgument<string>("str1");
        var str2 = ctx.GetArgument<string>("str2");
        if (str1 == null && str2 == null)
            throw new ValidationError("Must provide str1 or str2");
    });
```

Please throw a `ValidationError` exception or call `ctx.ReportError` to return a validation error to
the client. Throwing `ExecutionError` will prevent further validation rules from being executed,
and throwing other exceptions will be caught by the unhandled exception handler. This is different
than the `Parser` and `Validator` delegates, or scalar coercion methods, which will not trigger the
unhandled exception handler.

In version 8.1 and newer, you may use the `[ValidateArguments]` attribute to define a custom argument
validator as shown in the below example:

```csharp
// sample for argument validation
public class OutputClass3
{
    public static string Hello1(string str1, string str2) => str1 + str2;

    [ValidateArguments(nameof(ValidateHelloArguments))]
    public static string Hello2(string str1, string str2) => str1 + str2;

    private static ValueTask ValidateHelloArguments(FieldArgumentsValidationContext context)
    {
        var str1 = context.GetArgument<string>("str1");
        var str2 = context.GetArgument<string>("str2");
        if (str1 == null && str2 == null)
            context.ReportError("Must provide str1 or str2");
        return default;
    }
}
```

### 7. `@pattern` custom directive added for validating input values against a regular expression pattern

This directive allows for specifying a regular expression pattern to validate the input value.
It can also be used as sample code for designing new custom directives, and is now the preferred
design over the older `InputFieldsAndArgumentsOfCorrectLength` validation rule.
This directive is not enabled by default, and must be added to the schema as follows:

```csharp
services.AddGraphQL(b => b
    .AddSchema<MyQuery>()
    .ConfigureSchema(s =>
    {
        // add the directive to the schema
        s.Directives.Register(new PatternMatchingDirective());

        // add the visitor to the schema, which will apply validation rules to all field
        // arguments and input fields that have the @pattern directive applied
        s.RegisterVisitor(new PatternMatchingVisitor());
    }));
```

You can then apply the directive to any input field or argument as follows:

```csharp
Field(x => x.FirstName)
    .ApplyDirective("pattern", "regex", "[A-Z]+"); // uppercase only
```

### 8. DirectiveAttribute added to support applying directives to type-first graph types and fields

For example:

```csharp
private class Query
{
    public static string Hello(
        [Directive("pattern", "regex", "[A-Z]+")] // uppercase only
        string arg)
        => arg;
}
```

### 9. Validation rules can read or validate field arguments and directive arguments

Validation rules can now execute validation code either before or after field arguments
have been read. This is useful for edge cases, such as when a complexity analyzer needs
to read the value of a field argument to determine the complexity of the field.

The `ValidateAsync` method on `IValidationRule` has been changed to `GetPreNodeVisitorAsync`,
and a new method `GetPostNodeVisitorAsync` has been added. Also, the `IVariableVisitorProvider`
interface has been combined with `IValidationRule` and now has a new method `GetVariableVisitorAsync`.
So the new `IValidationRule` interface looks like this:

```csharp
public interface IValidationRule
{
    ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context);
    ValueTask<IVariableVisitor?> GetVariableVisitorAsync(ValidationContext context);
    ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context);
}
```

This allows for a single validation rule to validate AST structure, validate variable values,
and/or validate coerced field and directive arguments.

To simplify the creation of validation rules, the abstract `ValidationRuleBase` class has
been added, which implements the `IValidationRule` interface and provides default implementations
for all three methods.

Documentation has been added to the [Query Validation](https://graphql-dotnet.github.io/docs/getting-started/query-validation/)
section of the documentation to explain how to create custom validation rules using the
revised `IValidationRule` interface and related classes.

### 10. List coercion can be customized

Previously only specific list types were natively supported, such as `List<T>` and `IEnumerable<T>`,
and list types that implemented `IList`. Now, any list-like type such as `HashSet<T>` or `Queue<T>`
which has either a public parameterless constructor along with an `Add(T value)` method, or a constructor
that takes an `IEnumerable<T>` is supported. This allows for more flexibility in the types of lists
that can be used as CLR input types.

You can also register a custom list coercion provider to handle custom list types. For instance, if you
wish to use a case-insensitive comparer for `HashSet<string>` types, you can register a custom list coercion
provider as follows:

```csharp
// register for HashSet<string>
ValueConverter.RegisterListConverter<HashSet<string>, string>(
    values => new HashSet<string>(values, StringComparer.OrdinalIgnoreCase));

// also register for ISet<string>
ValueConverter.RegisterListConverter<ISet<string>, string>(
    values => new HashSet<string>(values, StringComparer.OrdinalIgnoreCase));
```

The `RegisterListProvider` method is also useful in AOT scenarios to provide ideal performance since dynamic
code generation is not possible, and to prevent trimming of necessary list types.

You can also register a custom list coercion provider for an open generic type. For instance, if you wish to
provide a custom list coercion provider for `IImmutableList<T>`, you can register it as follows:

```csharp
public class ImmutableListConverterFactory : ListConverterFactoryBase
{
    public override Func<object?[], object> Create<T>()
        => list => ImmutableList.CreateRange(list.Cast<T>());
}

ValueConverter.RegisterListConverterFactory(typeof(IImmutableList<>), new ImmutableListConverterFactory());
```

Finally, if you simply need to map an interface list type to a concrete list type, you can do so as follows:

```csharp
ValueConverter.RegisterListConverterFactory(typeof(IList<>), typeof(List<>)); // default mapping is T[]
```

### 11. `IGraphType.IsPrivate` and `IFieldType.IsPrivate` properties added

Allows to set a graph type or field as private within a schema visitor, effectively removing it from the schema.
Introspection queries will not be able to query the type/field, and queries will not be able to reference the type/field.
Exporting the schema as a SDL (or printing it) will not include the private types or fields.

Private types are fully 'resolved' and validated; you can obtain references to these types or fields in a schema validation
visitor before they are removed from the schema. After initialization is complete, these types and fields will not be present
within SchemaTypes or TypeFields. The only exception for validation is that private types are not required have any fields
or, for interfaces and unions, possible types.

This makes it possible to create a private type used within the schema but not exposed to the client. For instance,
it is possible to dynamically create input object types to deserialize GraphQL Federation entity representations, which
are normally sent via the `_Any` type.

### 12. `IObjectGraphType.SkipTypeCheck` property added

Allows to skip the type check for a specific object graph type during resolver execution. This is useful
for schema-first schemas where the CLR type is not defined while the resolver is built, while allowing
`IsTypeOf` to be set automatically for other use cases. Schema-first schemas will automatically set this
property to `true` for all object graph types to retain the existing behavior.

### 13. `ISchemaNodeVisitor.PostVisitSchema` method added

Allows to revisit the schema after all other methods (types/fields/etc) have been visited.

### 14. GraphQL Federation v2 graph types added

These graph types have been added to the `GraphQL.Federation.Types` namespace:

- `AnyScalarType` (moved from `GraphQL.Utilities.Federation`)
- `EntityGraphType`
- `FieldSetGraphType`
- `LinkImportGraphType`
- `LinkPurpose` enumeration
- `LinkPurposeGraphType`
- `ServiceGraphType`

### 15. Extension methods and attributes added to simplify applying GraphQL Federation directives in code-first and type-first schemas

These extension methods and attributes simplify the process of applying GraphQL Federation directives:

| Directive | Extension Method | Attribute | Description |
|-----------|------------------|-----------|-------------|
| `@external` | `External()` | `[External]` | Indicates that this subgraph usually can't resolve a particular object field, but it still needs to define that field for other purposes. |
| `@requires` | `Requires(fields)` | `[Requires(fields)]` | Indicates that the resolver for a particular entity field depends on the values of other entity fields that are resolved by other subgraphs. This tells the router that it needs to fetch the values of those externally defined fields first, even if the original client query didn't request them. |
| `@provides` | `Provides(fields)` | `[Provides(fields)]` | Specifies a set of entity fields that a subgraph can resolve, but only at a particular schema path (at other paths, the subgraph can't resolve those fields). |
| `@key` | `Key(fields)` | `[Key(fields)]` | Designates an object type as an entity and specifies its key fields. Key fields are a set of fields that a subgraph can use to uniquely identify any instance of the entity. |
| `@override` | `Override(from)` | `[Override(from)]` | Indicates that an object field is now resolved by this subgraph instead of another subgraph where it's also defined. This enables you to migrate a field from one subgraph to another. |
| `@shareable` | `Shareable()` | `[Shareable]` | Indicates that an object type's field is allowed to be resolved by multiple subgraphs (by default in Federation 2, object fields can be resolved by only one subgraph). |
| `@inaccessible` | `Inaccessible()` | `[Inaccessible]` | Indicates that a definition in the subgraph schema should be omitted from the router's API schema, even if that definition is also present in other subgraphs. This means that the field is not exposed to clients at all. |

### 16. OneOf Input Object support added

OneOf Input Objects are a special variant of Input Objects where the type system
asserts that exactly one of the fields must be set and non-null, all others
being omitted. This is useful for representing situations where an input may be
one of many different options.

See: https://github.com/graphql/graphql-spec/pull/825

To use this feature:
- **Code-First**: Set the `IsOneOf` property on your `InputObjectGraphType` to `true`.
- **Schema-First**: Use the `@oneOf` directive on the input type in your schema definition.
- **Type-First**: Use the `[OneOf]` directive on the CLR class.

Note: the feature is still a draft and has not made it into the official GraphQL spec yet.
It is expected to be added once it has been implemented in multiple libraries and proven to be useful.
It is not expected to change from the current draft.

### 17. Federation entity resolver configuration methods and attributes added for code-first and type-first schemas

Extension methods have been added for defining entity resolvers in code-first and type-first schemas
for GraphQL Federation.

Code-first sample 1: (uses entity type for representation)

```csharp
public class WidgetType : ObjectGraphType<Widget>
{
    public WidgetType()
    {
        // configure federation key fields
        this.Key("id");

        // configure federation resolver
        this.ResolveReference(async (context, widget) =>
        {
            // pull the id from the representation
            var id = widget.Id;

            // resolve the entity reference
            var widgetData = context.RequestServices!.GetRequiredService<WidgetRepository>();
            return await widgetData.GetWidgetByIdAsync(id, context.CancellationToken);
        });

        // configure fields
        Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
        Field(x => x.Name);
    }
}

public class Widget
{
    public string Id { get; set; }
    public string Name { get; set; }
}
```

Code-first sample 2: (uses custom type for representation)

```csharp
public class WidgetType : ObjectGraphType<Widget>
{
    public WidgetType()
    {
        // configure federation key fields
        this.Key("id");

        // configure federation resolver
        this.ResolveReference<WidgetRepresentation, Widget>(async (context, widget) =>
        {
            // pull the id from the representation
            var id = widget.Id;

            // resolve the entity reference
            var widgetData = context.RequestServices!.GetRequiredService<WidgetRepository>();
            return await widgetData.GetWidgetByIdAsync(id, context.CancellationToken);
        });

        // configure fields
        Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
        Field(x => x.Name);
    }
}

public class Widget
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class WidgetRepresentation
{
    public string Id { get; set; }
}
```

Type-first sample 1: (static method; uses method arguments for representation)

```csharp
// configure federation key fields
[Key("id")]
public class Widget
{
    // configure fields
    [Id]
    public string Id { get; set; }
    public string Name { get; set; }

    // configure federation resolver
    [FederationResolver]
    public static async Task<Widget> ResolveReference([FromServices] WidgetRepository widgetData, [Id] string id, CancellationToken token)
    {
        // resolve the entity reference
        return await widgetData.GetWidgetByIdAsync(id, token);
    }
}
```

Type-first sample 2: (instance method; uses instance for representation)

```csharp
// configure federation key fields
[Key("id")]
public class Widget
{
    // configure fields
    [Id]
    public string Id { get; set; }
    public string Name { get; set; }

    // configure federation resolver
    [FederationResolver]
    public async Task<Widget> ResolveReference([FromServices] WidgetRepository widgetData, CancellationToken token)
    {
        // pull the id from the representation
        var id = Id;

        // resolve the entity reference
        return await widgetData.GetWidgetByIdAsync(id, token);
    }
}
```

Note that you may apply the `[Key]` attribute multiple times to define multiple sets of key fields, pursuant to the
GraphQL Federation specification. You may define multiple resolvers when using static methods in a type-first schema.
Otherwise your method will need to decide which set of key fields to use for resolution, as demonstrated in the
code-first sample below:

```csharp
public class WidgetType : ObjectGraphType<Widget>
{
    public WidgetType()
    {
        // configure federation key fields
        this.Key("id");
        this.Key("sku");

        // configure federation resolver
        this.ResolveReference(async (context, widget) =>
        {
            // pull the key values from the representation
            var id = widget.Id;
            var sku = widget.Sku;

            // resolve the entity reference
            var widgetData = context.RequestServices!.GetRequiredService<WidgetRepository>();
            if (id != null)
                return await widgetData.GetWidgetByIdAsync(id, context.CancellationToken);
            else
                return await widgetData.GetWidgetBySkuAsync(sku, context.CancellationToken);
        });

        // configure fields
        Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
        Field(x => x.Sku);
        Field(x => x.Name);
    }
}

public class Widget
{
    public string Id { get; set; }
    public string Sku { get; set; }
    public string Name { get; set; }
}
```

### 18. Applied directives may contain metadata

`AppliedDirective` now implements `IProvideMetadata`, `IMetadataReader` and `IMetadataWriter`
to allow for reading and writing metadata to applied directives.

### 19. Support added for the Apollo `@link` directive

This directive indicates that some types and/or directives are to be imported from another schema.
Types and directives can be explicitly imported, either with their original name or with an alias.
Any types or directives that are not explicitly imported will be assumed to be named with a specified
namespace, which is derived from the URL of the linked schema if not set explicitly.
Visit https://specs.apollo.dev/link/v1.0/ for more information.

To link another schema, use code like this in your schema constructor or `ConfigureSchema` call:

```csharp
schema.LinkSchema("https://specs.apollo.dev/federation/v2.3", o =>
{
    // override the default namespace of 'federation' with the alias 'fed'
    o.Namespace = "fed";

    // import the '@key' directive without an alias
    o.Imports.Add("@key", "@key");

    // import the '@shareable' directive with an alias of '@share'
    o.Imports.Add("@shareable", "@share");

    // other directives such as '@requires' would be implicitly imported
    // into the 'fed' namespace, so '@requires' becomes '@fed__requires'
});
```

In addition to applying a `@link` directive to the schema, it will also import the `@link` directive
and configure the necessary types and directives to support the `@link` specification.
Your schema will then look like this:

```graphql
schema
  @link(url: "https://specs.apollo.dev/link/v1.0", import: ["@link"])
  @link(url: "https://specs.apollo.dev/federation/v2.3", as: "fed", import: ["@key", {name:"@shareable", as:"@share"}]) {
    # etc
}

directive @link(url: String!, as: String, import: [link__Import], purpose: link__Purpose) repeatable on SCHEMA

scalar link__Import

enum link__Purpose {
  EXECUTION
  SECURITY
}
```

You will still be required to add the imported schema definitions to your schema, such as `@key`, `@share`, and
`@fed__requires` in the above example. You may also print the schema without imported definitions. To print the
schema without imported definitions, set the `IncludeImportedDefinitions` option to `false` when printing:

```csharp
var sdl = schema.Print(new() { IncludeImportedDefinitions = false });
```

The schema shown above would now print like this:

```graphql
schema
  @link(url: "https://specs.apollo.dev/link/v1.0", import: ["@link"])
  @link(url: "https://specs.apollo.dev/federation/v2.3", as: "fed", import: ["@key", {name:"@shareable", as:"@share"}]) {
    # etc
}
```

Note that you may call `LinkSchema` multiple times with the same URL to apply additional configuration
options to the same url, or with a separate URL to link multiple schemas.

### 20. `FromSchemaUrl` added to `AppliedDirective`

This property supports using a directive that was separately imported via `@link`. After importing the schema as described
above, apply imported directives to your schema similar to the example below:

```csharp
graphType.ApplyDirective("shareable", s => s.FromSchemaUrl = "https://specs.apollo.dev/federation/"); // applies to any version
// or
graphType.ApplyDirective("shareable", s => s.FromSchemaUrl = "https://specs.apollo.dev/federation/v2.3"); // only version 2.3
```

During schema initialization, the name of the applied directive will be resolved to the fully-qualified name.
In the above example, if `@shareable` was imported, the directive will be applied as `@shareable`, but if not, it will
be applied as `@federation__shareable`. Aliases are also supported.

### 21. `AddFederation` GraphQL builder call added to initialize any schema for federation support

This method will automatically add the necessary types and directives to support GraphQL Federation.
Simply call `AddFederation` with the version number of the Federation specification that you wish to import
within your DI configuration. See example below:

```csharp
services.AddGraphQL(b => b
    .AddSchema<MySchema>()
    .AddFederation("2.3")
);
```

This will do the following:

- Configure the Query type to include the `_service` field.
- Configure the `_Entity` type based on which of the schema's type definitions are marked with `@key`.
- Configure the Query type to include the `_entities` field if there are any resolvable entities.
- Link the schema to the Federation specification at `https://specs.apollo.dev/federation/v2.3`.
- Import the `@key`, `@requires`, `@provides`, `@external`, `@extends`, `@shareable`, `@inaccessible`, `@override` and `@tag` directives.
- Configure the remaining supported directives within the `federation` namespace - `@federation__interfaceObject` and `@federation__composeDirective`
  for version 2.3.

Currently supported are versions 1.0 through 2.8. Note that for version 1.0, you will be required to mark
parts of your schema with `@extends`. This is not required for version 2.0 and later.

You may add additional configuration to the `AddFederation` call to import additional directives or types, remove imports,
change import aliases, or change the namespace used for directives that are not explicitly imported.

### 22. Infer field nullability from NRT annotations is enabled by default

When defining the field with expression, the graph type nullability will be inferred from
Null Reference Types (NRT) by default. To disable the feature, set the
`GlobalSwitches.InferFieldNullabilityFromNRTAnnotations` to `false`.

For example, given the following code

```c#
public class Person
{
    public string FullName { get; set; }
    public string? SpouseName { get; set; }
    public IList<string>? Children { get; set; }
}

public class PersonGraphType : ObjectGraphType<Person>
{
    public PersonGraphType()
    {
        Field(p => p.FullName);
        Field(p => p.SpouseName);
        Field(p => p.Children);
    }
}
```

When `InferFieldNullabilityFromNRTAnnotations` is `true` (default), the result is:

```graphql
type Person {
  fullName: String!
  spouseName: String
  children: [String!]
}
```

When `InferFieldNullabilityFromNRTAnnotations` is `false`:

```graphql
type Person {
  fullName: String!
  spouseName: String!
  children: [String]!
}
```

### 23. `ValidationContext.GetRecursivelyReferencedFragments` updated with `@skip` and `@include` directive support

When developing a custom validation rule, such as an authorization rule, you may need to determine which fragments are
recursively referenced by an operation by calling `GetRecursivelyReferencedFragments` with the `onlyUsed` argument
set to `true`. The method will then ignore fragments that are conditionally skipped by the `@skip` or `@include`
directives.

### 24. Persisted Document support

GraphQL.NET now supports persisted documents based on the draft spec [listed here](https://github.com/graphql/graphql-over-http/pull/264).
Persisted documents are a way to store a query string on the server and reference it by a unique identifier, typically
a SHA-256 hash. When enabled, the default configuration disables use of the `query` field in the request body and
requires the client to use the `documentId` field instead. This acts as a whitelist of allowed queries and mutations
that the client may execute, while also reducing the size of the request body.

To configure persisted document support, you must implement the `IPersistedDocumentLoader` interface to retrieve the
query string based on the document identifier, or set the `GetQueryDelegate` property on the `PersistedDocumentOptions`
class. See typical examples below:

#### Example 1 - Using a service to retrieve persisted documents

In the below example, regular requests (via the query property) are disabled, and only document identifiers
prefixed with `sha256:` are allowed where the id is a 64-character lowercase hexadecimal string.

```csharp
// configure the execution to utilize persisted documents
services.AddGraphQL(b => b
    // use default configuration, which disables the 'query' field and only allows SHA-256 hashes
    .UsePeristedDocuments<MyLoader>(GraphQL.DI.ServiceLifetime.Scoped)
);

// configure a service to retrieve persisted documents based on their hash
public class MyLoader : IPersistedDocumentLoader
{
    // pull in dependencies via DI as needed

    public async ValueTask<string?> GetQueryAsync(string? documentIdPrefix, string documentIdPayload, CancellationToken cancellationToken)
    {
        return await _db.QueryDocuments
            .Where(x => x.Hash == documentIdPayload)
            .Select(x => x.Query)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
```

Sample request:

```json
{
  "documentId": "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
  "variables": {
    "id": "1"
  }
}
```

#### Example 2 - Configuring persisted documents with the options class

In the below example, regular requests are allowed, and document identifiers are unprefixed GUIDs.

```csharp
// configure the execution to utilize persisted documents
services.AddGraphQL(b => b
    .UsePeristedDocuments(options =>
    {
        // enable regular queries also
        options.AllowNonpersistedDocuments = true;
        // use custom document identifiers and disable sha256 prefixed identifiers
        options.AllowedPrefixes.Clear()
        options.AllowedPrefixes.Add(null); // unprefixed document identifiers
        // configure the service to retrieve persisted documents
        options.GetQueryDelegate = async (executionOptions, documentIdPrefix, documentIdPayload) =>
        {
            if (!Guid.TryParse(documentIdPayload, out var id))
                return null;
            var db = executionOptions.RequestServices!.GetRequiredService<MyDbContext>();
            return await db.QueryDocuments
                .Where(x => x.Id == id)
                .Select(x => x.Query)
                .FirstOrDefaultAsync(executionOptions.CancellationToken);
        };
    });
);
```

Sample persisted document request:

```json
{
  "documentId": "01234567-89ab-cdef-0123-456789abcdef",
  "variables": {
    "id": "1"
  }
}
```

### 24. Execution timeout support

`ExecutionOptions.Timeout` has been added to allow a maximum time for the execution of a query. If the execution
exceeds the timeout, the execution will be cancelled and a timeout error will be returned to the client. The default
value is `Timeout.InfiniteTimeSpan`, which means no timeout is set. The timeout error is not an 'unhandled' exception
and so is not passed through the `UnhandledExceptionDelegate`.

Configuration:

```csharp
// via the GraphQL builder
services.AddGraphQL(b => b
    .WithTimeout(TimeSpan.FromSeconds(30))
);

// or via the options
options.Timeout = TimeSpan.FromSeconds(30);
```

Example response:

```json
{
    "errors":[
        {
            "message": "The operation has timed out.",
            "extensions": { "code": "TIMEOUT", "codes": [ "TIMEOUT" ] }
        }
    ]
}
```

Note that the timeout triggers immediate cancellation throughout the execution pipeline, including
resolvers and middleware. This means that any pending response data will be discarded as the client
would otherwise receive an incomplete response.

You may alternatively configure the timeout to throw a `TimeoutException` to the caller. Set the
`TimeoutAction` property as shown here:

```csharp
// via the GraphQL builder
services.AddGraphQL(b => b
    .WithTimeout(TimeSpan.FromSeconds(30), TimeoutAction.ThrowTimeoutException)
);

// or via the options
options.Timeout = TimeSpan.FromSeconds(30);
options.TimeoutAction = TimeoutAction.ThrowTimeoutException;
```

If you wish to catch and handle timeout errors, use the delegate overload as demonstrated below:

```csharp
services.AddGraphQL(b => b
    .WithTimeout(TimeSpan.FromSeconds(30), options =>
    {
        // log the timeout error
        var logger = options.RequestServices!.GetRequiredService<ILogger<MySchema>>();
        logger.LogError("The operation has timed out.");
        // return a custom error
        return new ExecutionResult(new TimeoutError());
    })
);
```

Please note that signaling the cancellation token passed to `ExecutionOptions.CancellationToken` will always
rethrow the `OperationCanceledException` to the caller, regardless of the `TimeoutAction` setting.

### 25. The complexity analyzer has been rewritten to support more complex scenarios

Please review the documentation for the new complexity analyzer to understand how to use it and how to configure it.
See the [Complexity Analzyer](../../guides/complexity-analyzer) document for more information.

### 26. Optimization when returning lists of scalars

When returning a list of scalars, specifically of intrinsic types such as `int`, `string`, and `bool`, the matching
scalar type (e.g. `IntGraphType`, `StringGraphType`, `BooleanGraphType`) will be used to serialize the list as a whole
rather than each individual item. This can result in a significant performance improvement when returning large lists
of scalars. Be sure the returned list type (e.g. `IEnumerable<int>`) matches the scalar type (e.g. `IntGraphType`)
to take advantage of this optimization. Scalar types that require conversion, such as `DateTimeGraphType` are not
currently optimized in this way.

### 27. GraphQL interfaces may implement other interfaces

Pursuant to the current GraphQL specification, interfaces may implement other interfaces. Add implemented interfaces
to your interface type definition as done for object graph types, as shown below:

Schema-first:

```graphql
interface Node {
  id: ID!
}

interface Character implements Node {
  id: ID!
  name: String!
}
```

Code-first:

```csharp
public class NodeGraphType : InterfaceGraphType
{
    public NodeGraphType()
    {
        Field<NonNullGraphType<IdGraphType>>("id");
    }
}

public class CharacterGraphType : InterfaceGraphType
{
    public CharacterGraphType()
    {
        Field<NonNullGraphType<IdGraphType>>("id");
        Field<NonNullGraphType<StringGraphType>>("name");
        Interface<NodeGraphType>();
    }
}
```

Type-first:

```csharp
public interface Node
{
    string Id { get; }
}
[Implements(typeof(Node))]
public interface Character : Node
{
    string Name { get; }
}
```

### 28. `IResolveFieldContext.ExecutionContext` property added

The `ExecutionContext` property has been added to the `IResolveFieldContext` interface to allow access to the
underlying execution context. This is useful for accessing the parsed arguments and directives from the operation
via `IExecutionContext.GetArguments` and `GetDirectives`.

### 29. `[DefaultAstValue]` attribute added (v8.1 and newer) to set default values for complex types with type-first schemas

When defining an input object or output field argument in a type-first schema, you may now use the `[DefaultAstValue]`
attribute to specify a default value for the argument. This is useful when the argument is a complex type that cannot
be represented as a constant via `[DefaultValue]` or in the method signature.

```csharp
// typical way to set a default value of an input field, which is not possible for complex types
public class MyInputObject1
{
    [DefaultValue("value")]
    public required string Field1 { get; set; }
}

// demonstrates setting a default value for an input field that has a complex type
public class MyInputObject2
{
    [DefaultAstValue("{ field1: \"value\" }")]
    public MyInputObject1 Json { get; set; }
}

// output type
public class MyOutputObject
{
    // typical way to set a default value of an output field argument
    public string Field1(string arg = "abc") => arg;

    // demonstrates setting a default value for an output field argument that has a complex type
    public string Field2([DefaultAstValue("{ field1: \"sample2\" }")] MyInputObject1 arg) => arg.Field1;
}
```

## Breaking Changes

### 1. Query type is required

Pursuant to the GraphQL specification, a query type is required for any schema.
This is enforced during schema validation but may be bypassed as follows:

```csharp
GlobalSwitches.RequireRootQueryType = false;
```

Future versions of GraphQL.NET will not contain this property and each schema will always be required to have a root Query type to comply with the GraphQL specification.

### 2. Use `ApplyDirective` instead of `Directive` on field builders

The `Directive` method on field builders has been renamed to `ApplyDirective` to better fit with
other field builder extension methods.

### 3. Use `WithComplexityImpact` instead of `ComplexityImpact` on field builders

The `ComplexityImpact` method on field builders has been renamed to `WithComplexityImpact` to better fit with
other field builder extension methods.

### 4. Relay types must be registered within the DI provider

Previuosly the Relay graph types were instantiated directly by the `SchemaTypes` class. This has been changed so that
the types are now pulled from the DI container. No changes are required if you are using the provided DI builder methods,
as they automatically register the relay types. Otherwise, you will need to manually register the Relay graph types.

```csharp
// v7 and prior -- builder methods -- NO CHANGES REQUIRED
services.AddGraphQL(b => {
  b.AddSchema<StarWarsSchema>();
});

// v7 and prior -- manual registration
services.AddSingleton<StarWarsSchema>(); // and other types

// v8
services.AddSingleton<PageInfoType>();
services.AddSingleton(typeof(EdgeType<>);
services.AddSingleton(typeof(ConnectionType<>);
services.AddSingleton(typeof(ConnectionType<,>);
```

### 5. Duplicate GraphQL configuration calls with the same `Use` command is ignored

Specifically, this relates to the following methods:

- `UseMemoryCache()`
- `UseAutomaticPersistedQueries()`
- `UseConfiguration<T>()` with the same `T` type

This change was made to prevent duplicate registrations of the same service within the DI container.

### 6. `ObjectExtensions.ToObject` changes (impacts `InputObjectGraphType`)

- `ObjectExtensions.ToObject<T>` was removed; it was only used by internal tests.
- `ObjectExtensions.ToObject` requires input object graph type for conversion.
- Only public constructors are eligible candidates while selecting a constructor.
- Constructor is selected based on the following rules:
  - If only a single public constructor is available, it is used.
  - If a public constructor is marked with `[GraphQLConstructor]`, it is used.
  - Otherwise the public parameterless constructor is used if available.
  - Otherwise an exception is thrown during deserialization.
- Only public properties are eligible candidates when matching a property.
- Any init-only or required properties not provided in the dictionary are set to their default values.
- Only public writable fields are eligible candidates when matching a field.

The changes above allow for matching behavior with source-generated or dynamically-compiled functions.

### 7. `AutoRegisteringInputObjectGraphType` changes

- See above changes to `ObjectExtensions.ToObject` for deserialization notes.
- Registers read-only properties when the property name matches the name of a parameter in the
  chosen constructor. Comparison is case-insensitive, matching `ToObject` behavior.
  Does not register constructor parameters that are not read-only properties.
  Any attributes such as `[Id]` must be applied to the property, not the constructor parameter.

### 8. Default naming of generic graph types has changed

The default graph name of generic types has changed to include the generic type name.
This should reduce naming conflicts when generics are in use. To consolidate behavior
across different code paths, both `Type` and `GraphType` are stripped from the end
of the class name. See below examples:

| Graph type class name | Old graph type name | New graph type name |
|------------------|---------------------|---------------------|
| `PersonType` | `PersonType` | `Person` |
| `PersonGraphType` | `Person` | `Person` |
| `AutoRegisteringObjectGraphType<SearchResults<Person>>` | `SearchResults` | `PersonSearchResults` |
| `LoggerGraphType<Person, string>` | `Logger` | `PersonStringLogger` |
| `InputObjectGraphType<Person>` | `InputObject_1` | `PersonInputObject` |

To revert to the prior behavior, set the following global switch prior to creating your schema classes:

```csharp
using GraphQL;

GlobalSwitches.UseLegacyTypeNaming = true;
```

As usual, you are encouraged to set the name in the constructor of your class, or
immediately after construction, or for auto-registering types, via an attribute.
You can also set global attributes that will be applied to all auto-registering types
if you wish to define your own naming logic.

The `UseLegacyTypeNaming` option is deprecated and will be removed in GraphQL.NET v9.

### 9. DataLoader extension methods have been moved to the GraphQL namespace

This change simplifies using extension methods for the data loaders. You may need to
remove the `using GraphQL.DataLoader;` statement from your code to resolve any
compiler warnings, and/or add `using GraphQL;`.

### 10. The SchemaPrinter has been deprecated

Please see the v7 migration document regarding the new `schema.ToAST()` and
`schema.Print()` methods available for printing the schema (available since 7.6).

For federated schemas, the `ServiceGraphType`'s `sdl` field will now use the
new implementation to print the schema. Please raise an issue if this causes
a problem for your federated schema.

### 11. Removed deprecated methods.

The following GraphQL DI builder methods have been removed:

| Method | Replacement |
|--------|-------------|
| `AddApolloTracing` | `UseApolloTracing` |
| `AddMiddleware` | `UseMiddleware` |
| `AddAutomaticPersistedQueries` | `UseAutomaticPersistedQueries` |
| `AddMemoryCache` | `UseMemoryCache` |

The following methods have been removed:

| Method | Comment |
|--------|---------|
| `TypeExtensions.IsConcrete` | Use `!type.IsAbstract` |
| `GraphQLTelemetryProvider.StartActivityAsync` | Use `StartActivity` |
| `AutoRegisteringInterfaceGraphType.BuildMemberInstanceExpression` | Interfaces cannot contain resolvers so this method was unused |
| `ValidationContext.GetVariableValuesAsync` | Use `GetVariablesValuesAsync` |

The following constructors have been removed:

| Class | Comment |
|-------|---------|
| `Variable` | Use new constructor with `definition` argument |
| `VariableUsage` | Use new constructor with `hasDefault` argument |

### 12. `IVariableVisitorProvider` removed and `IValidationRule` changed

The `ValidateAsync` method on `IValidationRule` has been changed to `GetPreNodeVisitorAsync`,
and a new method `GetPostNodeVisitorAsync` has been added. Also, the `IVariableVisitorProvider`
interface has been combined with `IValidationRule` and now has a new method `GetVariableVisitorAsync`.
So the new `IValidationRule` interface looks like this:

```csharp
public interface IValidationRule
{
    ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context);
    ValueTask<IVariableVisitor?> GetVariableVisitorAsync(ValidationContext context);
    ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context);
}
```

It is recommended to inherit from `ValidationRuleBase` for custom validation rules
and override only the methods you need to implement.

### 13. New properties added to `IGraphType`, `IFieldType` and `IObjectGraphType`

See the new features section for details on the new properties added to these interfaces.
Unless you directly implement these interfaces, you should not be impacted by these changes.

### 14. `ISchemaNodeVisitor.PostVisitSchema` method added

See the new features section for details on the new method added to this interface.
Unless you directly implement this interface, you should not be impacted by this change.

### 15. `AnyScalarType` and `ServiceGraphType` moved to `GraphQL.Federation.Types`

These graph types, previously located within the `GraphQL.Utilities.Federation` namespace,
have been moved to the `GraphQL.Federation.Types` namespace alongside all other federation types.

### 16. `IFederatedResolver`, `FuncFederatedResolver` and `ResolveReferenceAsync` replaced

- `IFederatedResolver` has been replaced with `IFederationResolver`.
- `FuncFederatedResolver` has been replaced with `FederationResolver`.
- `ResolveReferenceAsync` has been replaced with `ResolveReference`.

Please note that the new members are now located in the `GraphQL.Federation` namespace and may
require slight changes to your code to accommodate the new signatures. The old members have been
marked as obsolete and will continue to work in v8, but will be removed in v9.

### 17. GraphQL Federation entity resolvers do not automatically inject `__typename` into requests.

Previously, the `__typename` field was automatically injected into the request for entity resolvers.
This behavior has been removed as it is not required to meet the GraphQL Federation specification.

For instance, the following sample request:

```graphql
{
  _entities(representations: [{ __typename: "User", id: "1" }]) {
    ... on User {
      id
    }
  }
}
```

Should now be written as:

```graphql
{
  _entities(representations: [{ __typename: "User", id: "1" }]) {
    __typename
    ... on User {
      id
    }
  }
}
```

Please ensure that your client requests are updated to include the `__typename` field in the response.
Alternatively, you can install the provided `InjectTypenameValidationRule` validation rule to automatically
inject the `__typename` field into the request.

### 18. `IInputObjectGraphType.IsOneOf` property added

See the new features section for details on the new property added to this interface.
Unless you directly implement this interface, you should not be impacted by this change.

### 19. `VariableUsage.IsRequired` property added and `VariableUsage` constructor changed

This is required for OneOf Input Object support and is used to determine if a variable is required.
Unless you have a custom validation rule that uses `VariableUsage`, you should not be impacted
by this change.

### 20. Infer field nullability from NRT annotations is enabled by default

When defining the field with expression, the graph type nullability will be inferred from
Null Reference Types (NRT) by default. See the new features section for more details.  
To revert to old behavior set the global switch before initializing the schema

```csharp
GlobalSwitches.InferFieldNullabilityFromNRTAnnotations = false;
```

### 21. The complexity analyzer has been rewritten and functions differently

The complexity analyzer has been rewritten to be more efficient and to support more complex
scenarios. Please read the documentation on the new complexity analyzer to understand how it
works and how to configure it. To revert to the old behavior, use the `LegacyComplexityValidationRule`
or GraphQL builder method as follows:

```csharp
services.AddGraphQL(b => b
    .AddSchema<MyQuery>()
    .AddLegacyComplexityAnalyzer(c => c.MaxComplexity = 100)  // use the v7 complexity analyzer
);
```

The legacy complexity analyzer will be removed in v9.

### 22. Unhandled exceptions within execution configuration delegates are now handled and wrapped

Previously, unhandled exceptions within execution configuration delegates were not handled and
would be thrown directly to the caller. For instance, if a database exception occurred within
the delegate used to pull persisted documents, the exception would be thrown directly to the
caller. Now, these exceptions are caught and processed through the unhandled exception handler
delegate, which allows for logging and other processing of the exception.

Example:

```csharp
services.AddGraphQL(b => b
    .AddSchema<MyQuery>()
    .AddUnhandledExceptionHandler(context =>
    {
        // this now runs when the below exception is thrown
    })
    .ConfigureExecution(async (options, next) => {
        // sample exception thrown while logging the query being executed to a database
        throw new Exception("Database exception occurred");

        return await next(options);
    })
);
```

For this to work properly, be sure that any code liable to throw an exception is located inside
a `Use...` or `ConfigureExecution` method, not an `Add...` or `ConfigureExecutionOptions` method,
or else ensure that the call to `AddUnhandledExceptionHandler` is first in the chain.

### 23. `IExecutionContext.ExecutionOptions` property added

Custom `IExecutionContext` implementations must now implement the `ExecutionOptions` property.
In addition, the `AddUnhandledExceptionHandler` methods that have an `ExecutionOptions` parameter
within the delegate have been deprecated in favor of using the `IExecutionContext.ExecutionOptions`
property.

```csharp
// v7
services.AddGraphQL(b => b
    .AddSchema<MyQuery>()
    .AddUnhandledExceptionHandler((context, options) =>
    {
        // code
    })
);

// v8
services.AddGraphQL(b => b
    .AddSchema<MyQuery>()
    .AddUnhandledExceptionHandler(context =>
    {
        var options = context.ExecutionOptions;
        // code
    })
);
```

### 24. ID graph type serialization is culture-invariant

The `IdGraphType` now serializes values using the invariant culture.

### 25. DirectiveAttribute moved to GraphQL namespace

The `DirectiveAttribute` has been moved to the `GraphQL` namespace.

### 26. Changes to support interface inheritance

Small changes were made to `IInterfaceGraphType` and `IImplementInterfaces` to support interface inheritance.
If you have custom implementations of these interfaces, you may need to update them.

### 27. Stricter type checking when implementing interfaces

Verfies that the arguments defined on fields of interfaces are also defined on fields of implementing types,
pursuant to GraphQL specifications.

### 28. APIs marked obsolete since v5 have been removed

- `GraphQLMetadataAttribute.InputType` has been replaced with `InputTypeAttribute`.
- `GraphQLMetadataAttribute.OutputType` has been replaced with `OutputTypeAttribute`.
- `ErrorInfoProviderOptions.ExposeExceptionStackTrace` has been replaced with `ExposeExceptionDetails` and `ExposeExceptionDetailsMode`.

See prior migration documents for more details concerning these changes.

### 29. `IResolveFieldContext.ExecutionContext` added and `ExecutionContext.GetArguments` signature has changed

If you are using the `ExecutionContext.GetArguments` method to parse arguments directly, for example to retrieve
the arguments of child fields in a resolver, we suggest using the new `IExecutionContext.GetArguments` and
`IExecutionContext.GetDirectives` methods instead. These take advantage of the fact that all arguments
are parsed during the validation phase and need not be parsed again. See example below:

```csharp
// v7
IResolveFieldContext context;       // parent context
FieldType fieldDefinition;          // field definition from which to get arguments
GraphQLField fieldAst;              // field AST from which to get arguments
var arguments = ExecutionHelper.GetArguments(fieldDefinition.Arguments, fieldAst.Arguments, context.Variables);

// v8
IResolveFieldContext context;       // parent context
FieldType fieldDefinition;          // field definition from which to get arguments
GraphQLField fieldAst;              // field AST from which to get arguments
var arguments = context.ExecutionContext.GetArguments(fieldDefinition, fieldAst);
```

To continue to use the `ExecutionHelper.GetArguments` method, you may need to refer to the GraphQL.NET source
for reference.

If you directly implement `IResolveFieldContext`, you must now also implement the `ExecutionContext` property.

### 30. `GraphType.Initialize` method is now called after initialization is complete

The `Initialize` method on each `GraphType` is now called after the schema has been fully initialized. As such,
you cannot add fields to the graph type expecting `SchemaTypes` to resolve types and apply name converters.
If it is necessary for your graph type to add fields dynamically, you should do so in the constructor or else
set the `ResolvedType` property for the new fields. Failing to do so will result in a schema validation exception.

Please note that the constructor is the preferred place to add fields to a graph type.

```csharp
// v7
public class MyGraphType : ObjectGraphType
{
    public override void Initialize(ISchema schema)
    {
        AddField(new FieldType {
            Name = "Field",
            Type = typeof(StringGraphType)
        });
    }
}

// v8
public class MyGraphType : ObjectGraphType
{
    public override void Initialize(ISchema schema)
    {
        AddField(new FieldType {
            Name = "field", // name converter is not applied here, so the name must be exactly as desired
            ResolvedType = new StringGraphType()
        });
    }
}
```

## Appendix

### Schema verification test example

The below example demonstrates how to write a test to verify that the schema does not change during
migration.

```csharp
[Fact]
public void VerifyIntrospection()
{
    var services = new ServiceCollection();
    var startup = new Startup(new ConfigurationBuilder().Build());
    startup.ConfigureServices(services);
    var provider = services.BuildServiceProvider();

    var schema = provider.GetRequiredService<ISchema>();
    schema.Initialize();
    var sdl = schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase });

    sdl.ShouldMatchApproved(o => o.NoDiff().WithFileExtension("graphql"));
}
```

This test uses the `ShouldMatchApproved` method from the `Shouldly` NuGet package to compare the schema
generated by the application to the approved schema. If the schema changes, the test will fail, and you
will need to update the approved schema file or fix the code that caused the schema to change.
