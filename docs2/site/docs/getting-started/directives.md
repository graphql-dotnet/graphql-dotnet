# Directives

A directive can be attached to almost every part of the schema - field, query, enum, fragment inclusion etc. and can affect execution
of the query in any way the server desires. The core GraphQL [specification](https://graphql.github.io/graphql-spec/June2018/#sec-Type-System.Directives)
includes exactly three directives.

* `@include(if: Boolean!) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT` Only include this field in the result if the argument is true.
* `@skip(if: Boolean!) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT` Skip this field if the argument is true.
* `@deprecated(reason: String = "No longer supported") on FIELD_DEFINITION | ENUM_VALUE` Indicates deprecated portions of a GraphQL service’s schema, such as deprecated fields on a type or deprecated enum values.

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
on the schema (so called server directives). This is determined by the specified locations when defining the directive.
Also it is acceptable to define a directive that will be both client-side and server-side.

Server-side examples:
- [@deprecated](http://spec.graphql.org/June2018/#sec--deprecated)

Client-side examples:
- [@skip](http://spec.graphql.org/June2018/#sec--skip)
- [@include](http://spec.graphql.org/June2018/#sec--include)

# Defining your own directive

To define your own directive create a directive class inherited from `DirectiveGraphType`.

```csharp
public class MyDirective : DirectiveGraphType
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

Then register it withing your schema.

```csharp
public class MySchema : Schema
{
    public MySchema()
    {
        Directives.Register(new MyDirective());
    }
}
```

# Repeatable Directives

A directive may be defined as repeatable by including the `repeatable` keyword. Repeatable directives are often useful
when the same directive should be used with different arguments at a single location, especially in cases where additional
information needs to be provided to a type or schema extension via a directive. GraphQL.NET v4 supports repeatable directives.
To make your directive repeatable set `DirectiveGraphType.Repeatable` property to `true`.

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

Since v4 Graph.NET provides the ability to apply directives to the
schema elements and expose this user-defined meta-information via introspection. This is an experimental feature that
is not in the official specification (yet). To enable it call `ISchema.EnableExperimentalIntrospectionFeatures()`.
This method also enables ability to expose `isRepeatable` field for directives via introspection (feature from the
GraphQL specification working draft). Note that you can also set the `mode` parameter (`ExecutionOnly` by default).

```csharp
/// <summary>
/// A way to use experimental features.
/// </summary>
public enum ExperimentalIntrospectionFeaturesMode
{
    /// <summary>
    /// Allow experimental features only for client queries but not for standard introspection
    /// request. This means that the client, in response to a standard introspection request,
    /// receives a standard response without all the new fields and types. However, client CAN
    /// make requests to the server using the new fields and types. This mode is needed in order
    /// to bypass the problem of tools such as GraphQL Playground, Voyager, GraphiQL that require
    /// a standard response to an introspection request and refuse to work correctly if receive
    /// unknown fields or types in the response.
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
  value: String
}
```

To allow clients to get your defined directive along with all applications of this directive to the
schema elements through introspection, override the `Introspectable` property of your directive.

```csharp
public class MyDirective : DirectiveGraphType
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

If you do not explicitly set this property (either to `true` or `false`) then by default
your directive definition along with all applications of this directive to the schema elements
will be present in the introspection response if and only if it has all its locations of type
`ExecutableDirectiveLocation` (so called client-side directive).

> See https://github.com/graphql/graphql-spec/issues/300 for more information.

# How to apply a directive

After you have defined your directive, then it can be applied to the corresponding elements of the schema.
If you try to apply the directive in locations that are not allowed for this, an exception will be thrown
when initializing the schema.

The following is an example of using the `@length` directive defined as

```csharp
public class LengthDirective : DirectiveGraphType
{
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

Applying `@length` directive to an input field:

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

Applying `@length` directive to a field argument:

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

Also, during the schema initialization, the compliance of all applied directives with the corresponding
directives definitions (names, number and types of parameters, and so on) will be checked.
