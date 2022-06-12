# Directives

A directive can be attached to almost every part of the schema - field, query, enum, fragment inclusion etc. and can affect execution
of the query in any way the server desires. The core GraphQL [specification](https://spec.graphql.org/October2021/#sec-Type-System.Directives)
includes exactly four directives.

* `@include(if: Boolean!) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT` Only include this field in the result if the argument is true.
* `@skip(if: Boolean!) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT` Skip this field if the argument is true.
* `@deprecated(reason: String = "No longer supported") on FIELD_DEFINITION | ENUM_VALUE` Indicates deprecated portions of a GraphQL service’s schema, such as deprecated fields on a type or deprecated enum values.
* `@specifiedBy(url: String!) on SCALAR` Provides a scalar specification URL for specifying the behavior of custom scalar types.

```graphql
query HeroQuery($id: ID, $withFriends: Boolean!) {
  hero(id: $id) {
    name
    friends @include(if: $withFriends) {
      name
    }
  }
}
```

# Executable Directives and Type System Directives

There are two types of directives - those that are applied on incoming requests (so called client directives) and applied
on the schema (so called server directives). This is determined by the specified [locations](https://spec.graphql.org/October2021/#sec-Type-System.Directives)
when defining the directive. Also it is acceptable to define a directive that will be both client-side and server-side.

Server-side examples:
- [@deprecated](https://spec.graphql.org/October2021/#sec--deprecated)
- [@specifiedBy](https://spec.graphql.org/October2021/#sec--specifiedBy)

Client-side examples:
- [@skip](https://spec.graphql.org/October2021/#sec--skip)
- [@include](https://spec.graphql.org/October2021/#sec--include)

# Repeatable Directives

In GraphQL language a directive may be defined as repeatable by including the `repeatable` keyword.
Repeatable directives are often useful when the same directive should be used with different arguments
at a single location, especially in cases where additional information needs to be provided to a type
or schema extension via a directive. GraphQL.NET v4 supports repeatable directives. To make your directive
repeatable in GraphQL.NET set `Directive.Repeatable` property to `true`.

# Basic steps when adding a directive

1. Define your custom directive.
2. Apply the directive to the desired schema elements.
3. Write the code that will implement the logic of the directive.

# Defining your custom directive

To define your custom directive create a directive class inherited from `Directive`.

```csharp
public class MyDirective : Directive
{
    public MyDirective()
        : base("my", DirectiveLocation.Field, DirectiveLocation.FragmentSpread, DirectiveLocation.InlineFragment)
    {
        Description = "My super directive";
        Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>>
        {
            Name = "secret",
            Description = "Some secret"
        });
    }
}
```

In SDL this definition will look like the following:

```graphql
directive @my(secret: String!) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT
```

Then register an instance of this class within your schema.

```csharp
public class MySchema : Schema
{
    public MySchema()
    {
        Directives.Register(new MyDirective());
    }
}
```

# How to apply a directive

After you have defined your directive, then it can be applied to the corresponding elements of the schema.
If you try to apply the directive in locations that are not allowed for this, an exception will be thrown
when initializing the schema. Also, during the schema initialization, the compliance of all applied
directives with the corresponding directives' definitions (names, number and types of parameters, and so on)
will be checked.

The following is an example of using the server-side `@length` directive.

```csharp
public class LengthDirective : Directive
{
    // The meaning of this property will be explained below in the 'Directives and introspection' paragraph. 
    public override bool? Introspectable => true;

    public LengthDirective()
        : base("length", DirectiveLocation.InputFieldDefinition, DirectiveLocation.ArgumentDefinition)
    {
        Description = "Used to specify the minimum and/or maximum length for an input field or argument.";
        Arguments = new QueryArguments(
            new QueryArgument<IntGraphType>
            {
                Name = "min",
                Description = "If specified, specifies the minimum length that the input field or argument must have."
            },
            new QueryArgument<IntGraphType>
            {
                Name = "max",
                Description = "If specified, specifies the maximum length that the input field or argument must have."
            }
        );
    }
}
```

In SDL this definition will look like the following:

```graphql
directive @length(min: Int, max: Int) on INPUT_FIELD_DEFINITION | ARGUMENT_DEFINITION
```

Applying `@length` directive to an input field.

```csharp
public class ComplexInput : InputObjectGraphType
{
    public ComplexInput()
    {
        Name = "ComplexInput";
        Field<IntGraphType>("intField");
        Field<StringGraphType>("stringField").ApplyDirective("length", "min", 3, "max", 7);
    }
}
```

Applying `@length` directive to a field argument.

```csharp
public class Query : ObjectGraphType
{
    public Query()
    {
        Field<Human>(
            "human",
            arguments: new QueryArguments(
                new QueryArgument<IdGraphType>
                {
                    Name = "id"
                }
                .ApplyDirective("length", "min", 2, "max", 5)
            ));
    }
}
```

Above was an example of how to create and how to apply the `@length` directive. Also, for the directive
to work, additional code is required that would use the requirements specified by the directive. The
implementation of this code for `@length` directive is intentionally omitted, due to the complexity,
those who interested can look at it in the [sources](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL/Validation/Rules.Custom/InputFieldsAndArgumentsOfCorrectLength.cs).
For a much simpler example of such an implementation, see [How do directives work](#how-do-directives-work)
paragraph below describing the `@upper` directive.

# How do directives work

So you have defined a directive. Then you have applied (or not in case of client-side directive) this
directive to the required locations in your schema. What's next? So far, all you have done is set some
meta information, and there is still no code anywhere that is responsible for the actions of the added
directive. The next step is to define a class that will customize the schema using the information
provided by the applied directive. This class should implement `ISchemaNodeVisitor` interface.

Let's imagine an `@upper` directive.

```csharp
public class UpperDirective : Directive
{
    public UpperDirective()
        : base("upper", DirectiveLocation.FieldDefinition)
    {
        Description = "Converts the value of string fields to uppercase.";
    }
}
```

In SDL this definition will look like the following:

```graphql
directive @upper on FIELD_DEFINITION
```

To make this directive work, you need to write a class like the following by implementing the necessary
schema visitor methods. `BaseSchemaNodeVisitor` is just a base class implementing `ISchemaNodeVisitor`
interface with empty `virtual` methods, so it does nothing. For this example, we need to override just
one method - `VisitFieldDefinition`. This method wraps the original field resolver.

```csharp
public class UppercaseDirectiveVisitor : BaseSchemaNodeVisitor
{
    public override void VisitFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema)
    {
        var applied = field.FindAppliedDirective("upper");
        if (applied != null)
        {
            var inner = field.Resolver ?? NameFieldResolver.Instance;
            field.Resolver = new AsyncFieldResolver<object>(async context =>
            {
                object result = await inner.ResolveAsync(context);

                return result is string str
                    ? str.ToUpperInvariant()
                    : result;
            });
        }
    }
}
```

And then register schema visitor within your schema just like you did to register the directive.

```csharp
public class MySchema : Schema
{
    public MySchema()
    {
        RegisterVisitor(new UppercaseDirectiveVisitor());

        // there are also registration methods that take the type, see below for details
        // RegisterVisitor(typeof(UppercaseDirectiveVisitor));
        // this.RegisterVisitor<UppercaseDirectiveVisitor>(); // extension method
    }
}
```

Note that a schema visitor, unlike a directive, can be registered not only as an instance but also as
a type. In this case, when initializing the schema, schema visitor will be created according to how
you configure the DI container. In other words, schema visitors support dependency injection. The
library resolves a schema visitor only once and caches it for the lifetime of the `Schema`. For more
information about lifetimes see [Schema Service Lifetime](dependency-injection#schema-service-lifetime). 

# Is it mandatory to create a schema visitor in addition to the directive

No. The applied directives (along with the directive definition itself) can exist without the corresponding
schema visitors. In this case, the directive is usually set to provide additional information to clients by
means of introspection. For example, consider such server-side `@author` directive:

```csharp
public class AuthorDirective : Directive
{
    public AuthorDirective()
        : base("author", DirectiveLocation.FieldDefinition)
    {
        Description = "Provides information about the author of the field";
        Arguments = new QueryArguments(
            new QueryArgument<StringGraphType>
            {
                Name = "name",
                Description = "Author's name"
            },
            new QueryArgument<NonNullGraphType<StringGraphType>>
            {
                Name = "email",
               Description = "Email where you can ask your question"
            }
        );
    }
}
```

In SDL this definition will look like the following:

```graphql
directive @author(name: String, email: String!) on FIELD_DEFINITION
```

Then the directive can be applied like this:

```csharp
public class Query : ObjectGraphType
{
    public Query()
    {
        Field<Human>("human", resolve: context => GetHuman(context))
            .ApplyDirective("author", "name", "Tom Pumpkin", "email", "ztx0673@gmail.com");
    }
}
```

As you can see, the GraphQL server simply provides additional information that is available to clients through introspection.
The GraphQL server does not assume any processing of it.

Another case is when the directive is not used by a corresponding schema visitor, but by another GraphQL.NET component, for
example, a validation rule. Consider the [@length](#How-to-apply-a-directive) directive example above. The purpose of this
directive for server is to validate inputs before executing a GraphQL request. The same can be said for a client - it wouldn't
make sense to send a request with data not within the declared length limits. That is, of course, if client is ready to
recognize a custom server-defined directive.

# Can a schema visitor be used without creating/registering a directive

Yes. Strictly speaking, schema visitors do not necessarily process directives. `ISchemaNodeVisitor` interface is a general
means of traversing a schema. You can traverse your schema at any time using the `Run` extension method. Just remember that
if your schema visitor modifies the schema, then you must ensure synchronization if you call `Run` method in parallel with
the processing of incoming GraphQL requests to the schema.

```csharp
var schema = new MySchema();
var visitor = new MyVisitor();
visitor.Run(schema);
```

# Directives and introspection

Currently, the GraphQL specification allows introspection only of directives defined in the schema but does not allow
introspection of so called _applied_ directives.

```graphql
type __Schema {
  description: String
  types: [__Type!]!
  queryType: __Type!
  mutationType: __Type
  subscriptionType: __Type
  directives: [__Directive!]!   <----- only defined directives here
}
```

Since v4 Graph.NET provides the ability to apply directives to the schema elements and expose this user-defined
meta-information via introspection. This is an experimental feature that is not in the official specification (yet).
To enable it call `ISchema.EnableExperimentalIntrospectionFeatures()`. This method also makes it possible to
expose directives' `isRepeatable` field via introspection (a feature from the GraphQL specification working draft).
Note that you can also set the `mode` parameter in this method which by default equals to `ExecutionOnly`.

```csharp
/// <summary>
/// A way to use experimental features.
/// </summary>
public enum ExperimentalIntrospectionFeaturesMode
{
    /// <summary>
    /// Allow experimental features only for client queries but not for standard introspection
    /// request. This means that the client, in response to a standard introspection request,
    /// receives a standard response without any new fields and types. However, client CAN
    /// make requests to the server using the new fields and types. This mode is needed in order
    /// to bypass the problem of tools such as GraphQL Playground, Voyager, GraphiQL that require
    /// a standard response to an introspection request and refuse to work correctly if there are
    /// any unknown fields or types in the response.
    /// </summary>
    ExecutionOnly,

    /// <summary>
    /// Allow experimental features for both standard introspection query and client queries.
    /// This means that the client, in response to a standard introspection request, receives
    /// a response augmented with the new fields and types. Client can make requests to the
    /// server using the new fields and types.
    /// </summary>
    IntrospectionAndExecution
}
```

Introspection schema after enabling experimental features (new types and fields are highlighted).

```graphql
type __Schema {
  description: String
  types: [__Type!]!
  queryType: __Type!
  mutationType: __Type
  subscriptionType: __Type
  directives: [__Directive!]!
  appliedDirectives: [__AppliedDirective!]!   <----- NEW FIELD
}

type __Type {
  kind: __TypeKind!
  name: String
  description: String
  fields(includeDeprecated: Boolean = false): [__Field!]
  interfaces: [__Type!]
  possibleTypes: [__Type!]
  enumValues(includeDeprecated: Boolean = false): [__EnumValue!]
  inputFields: [__InputValue!]
  ofType: __Type
  appliedDirectives: [__AppliedDirective!]!   <----- NEW FIELD
}

type __Field {
  name: String!
  description: String
  args: [__InputValue!]!
  type: __Type!
  isDeprecated: Boolean!
  deprecationReason: String
  appliedDirectives: [__AppliedDirective!]!   <----- NEW FIELD
}

type __InputValue {
  name: String!
  description: String
  type: __Type!
  defaultValue: String
  appliedDirectives: [__AppliedDirective!]!   <----- NEW FIELD
}

type __EnumValue {
  name: String!
  description: String
  isDeprecated: Boolean!
  deprecationReason: String
  appliedDirectives: [__AppliedDirective!]!   <----- NEW FIELD
}

enum __TypeKind {
  SCALAR
  OBJECT
  INTERFACE
  UNION
  ENUM
  INPUT_OBJECT
  LIST
  NON_NULL
}

type __Directive {
  name: String!
  description: String
  locations: [__DirectiveLocation!]!
  args: [__InputValue!]!
  isRepeatable: Boolean!                      <----- NEW FIELD (FROM THE WORKING DRAFT)
  appliedDirectives: [__AppliedDirective!]!   <----- NEW FIELD
}

enum __DirectiveLocation {
  QUERY
  MUTATION
  SUBSCRIPTION
  FIELD
  FRAGMENT_DEFINITION
  FRAGMENT_SPREAD
  INLINE_FRAGMENT
  SCHEMA
  SCALAR
  OBJECT
  FIELD_DEFINITION
  ARGUMENT_DEFINITION
  INTERFACE
  UNION
  ENUM
  ENUM_VALUE
  INPUT_OBJECT
  INPUT_FIELD_DEFINITION
}

type __AppliedDirective {                     <--- NEW INTROSPECTION TYPE
  name: String!
  args: [__DirectiveArgument!]!
}

type __DirectiveArgument {                    <--- NEW INTROSPECTION TYPE
  name: String!
  value: String!
}
```

To make your defined directive and all its applications to the schema elements available through
introspection, override the `Introspectable` property of your directive.

```csharp
public class MyDirective : Directive
{
    public MyDirective()
        : base("my", DirectiveLocation.Field, DirectiveLocation.FragmentSpread, DirectiveLocation.InlineFragment)
    {
        Description = "My super directive";
        Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>>
        {
            Name = "secret",
            Description = "Some secret"
        });
    }

    public override bool? Introspectable => true;
}
```

If you do not explicitly set this property (either to `true` or `false`) then by default your
directive definition along with all applications of this directive to the schema elements will
be present in the introspection response if and only if directive definition has all its locations
of type [`ExecutableDirectiveLocation`](https://spec.graphql.org/October2021/#ExecutableDirectiveLocation)
(so called client-side directive).

# Directive vs Field Middleware 

You can think of a Field Middleware as something global that controls how all fields of all types
in the schema are resolved. A directive, at the same time, would only affect specific schema elements
and only those elements. Moreover, a directive is not limited to field resolvers like middleware is.
For more information about field middlewares see [Field Middleware](https://graphql-dotnet.github.io/docs/getting-started/field-middleware).

# Existing implementations

There has long been a need in the community for a specification to describe the possibility of
getting _applied_ directives through introspection. An example is [issue-300](https://github.com/graphql/graphql-spec/issues/300)
(almost 4 years old at the time of this writing). Obviously, some projects couldn't wait any
longer and somehow added _applied_ directive support on their own. One such project is this one - GraphQL.NET.
The [graphql-java](https://github.com/graphql-java/graphql-java) project followed a [similar](https://github.com/graphql-java/graphql-java/pull/2221)
path. Perhaps there are others, the page will be updated.

We hope that this consistency helps the GraphQL world in the absence of a proper GraphQL specification
mechanism for getting _applied_ directives through introspection. If other projects on other platforms/languages
support _applied_ directives in this form, it will become a de facto standard and speed up the specification process.
