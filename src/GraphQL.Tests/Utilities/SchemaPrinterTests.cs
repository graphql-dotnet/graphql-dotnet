using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Conversion;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class SchemaPrinterTests
    {
        private const string built_in_scalars = @"
# The `Date` scalar type represents a timestamp provided in UTC. `Date` expects
# timestamps to be formatted in accordance with the
# [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.
scalar Date

scalar Decimal
";

        private string printSingleFieldSchema<T>(
            IEnumerable<QueryArgument> arguments = null)
            where T : GraphType
        {
            var args = arguments != null ? new QueryArguments(arguments) : null;

            var root = new ObjectGraphType();
            root.Name = "Query";
            root.Field<T>(
                "singleField",
                arguments: args);

            var schema = new Schema
            {
                Query = root
            };

            var result = print(schema);

            // ensure schema isn't disposed before test finishes
            if (schema.Query.Name == "")
            {
            }

            return result;
        }

        private string print(ISchema schema)
        {
            var printer = new SchemaPrinter(schema);
            return Environment.NewLine + printer.Print();
        }

        private void AssertEqual(string result, string expected, bool excludeScalars = false)
        {
            var exp = excludeScalars ? expected : built_in_scalars + expected;
            result.Replace("\r", "").ShouldBe(exp.Replace("\r", ""));
        }

        [Fact]
        public void prints_directive()
        {
            var printer = new SchemaPrinter(null);
            var arg = DirectiveGraphType.Skip.Arguments.First();
            arg.ResolvedType = arg.Type.BuildNamedType();

            var result = printer.PrintDirective(DirectiveGraphType.Skip);
            const string expected = @"# Directs the executor to skip this field or fragment when the 'if' argument is true.
directive @skip(
  if: Boolean!
) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT";

            AssertEqual(result, expected, excludeScalars: true);
        }

        [Fact]
        public void prints_string_field()
        {
            var result = printSingleFieldSchema<StringGraphType>();
            const string expected = @"
type Query {
  singleField: String
}
";
            AssertEqual(result, expected);
        }

        [Fact]
        public void prints_string_list_field()
        {
            var result = printSingleFieldSchema<ListGraphType<StringGraphType>>();
            const string expected = @"
type Query {
  singleField: [String]
}
";
            AssertEqual(result, expected);
        }

        [Fact]
        public void prints_non_null_string_field()
        {
            var result = printSingleFieldSchema<NonNullGraphType<StringGraphType>>();
            const string expected = @"
type Query {
  singleField: String!
}
";
            AssertEqual(result, expected);
        }

        [Fact]
        public void prints_non_null_list_of_string_field()
        {
            var result = printSingleFieldSchema<NonNullGraphType<ListGraphType<StringGraphType>>>();
            const string expected = @"
type Query {
  singleField: [String]!
}
";
            AssertEqual(result, expected);
        }

        [Fact]
        public void prints_non_null_list_of_non_null_string_field()
        {
            var result = printSingleFieldSchema<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>();
            const string expected = @"
type Query {
  singleField: [String!]!
}
";
            AssertEqual(result, expected);
        }

        [Fact]
        public void prints_object_field()
        {
            var root = new ObjectGraphType {Name = "Query"};
            root.Field<FooType>("foo");

            var schema = new Schema {Query = root};

            AssertEqual(print(schema), @"
type Foo {
  str: String
}

type Query {
  foo: Foo
}
");
        }

        [Fact]
        public void prints_string_field_with_int_arg()
        {
            var result = printSingleFieldSchema<StringGraphType>(
                new[]
                {
                    new QueryArgument<IntGraphType> {Name = "argOne"}
                });

            const string expected = @"
type Query {
  singleField(argOne: Int): String
}
";
            AssertEqual(result, expected);
        }

        [Fact]
        public void prints_string_field_with_int_arg_with_default()
        {
            var result = printSingleFieldSchema<StringGraphType>(
                new[]
                {
                    new QueryArgument<IntGraphType> {Name = "argOne", DefaultValue = 2}
                });

            const string expected = @"
type Query {
  singleField(argOne: Int = 2): String
}
";
            AssertEqual(result, expected);
        }

        [Fact]
        public void prints_string_field_with_non_null_int_arg()
        {
            var result = printSingleFieldSchema<StringGraphType>(
                new[]
                {
                    new QueryArgument<NonNullGraphType<IntGraphType>> {Name = "argOne"}
                });

            const string expected = @"
type Query {
  singleField(argOne: Int!): String
}
";
            AssertEqual(result, expected);
        }

        [Fact]
        public void prints_string_field_with_multiple_args()
        {
            var result = printSingleFieldSchema<StringGraphType>(
                new QueryArgument[]
                {
                    new QueryArgument<IntGraphType> {Name = "argOne"},
                    new QueryArgument<StringGraphType> {Name = "argTwo"}
                });

            const string expected = @"
type Query {
  singleField(argOne: Int, argTwo: String): String
}
";
            AssertEqual(result, expected);
        }

        [Fact]
        public void prints_string_field_with_multiple_args_first_has_default()
        {
            var result = printSingleFieldSchema<StringGraphType>(
                new QueryArgument[]
                {
                    new QueryArgument<IntGraphType> {Name = "argOne", DefaultValue = 1},
                    new QueryArgument<StringGraphType> {Name = "argTwo"},
                    new QueryArgument<BooleanGraphType> {Name = "argThree"}
                });

            const string expected = @"
type Query {
  singleField(argOne: Int = 1, argTwo: String, argThree: Boolean): String
}
";
            AssertEqual(result, expected);
        }

        [Fact]
        public void prints_string_field_with_multiple_args_second_has_default()
        {
            var result = printSingleFieldSchema<StringGraphType>(
                new QueryArgument[]
                {
                    new QueryArgument<IntGraphType> {Name = "argOne"},
                    new QueryArgument<StringGraphType> {Name = "argTwo", DefaultValue = "foo"},
                    new QueryArgument<BooleanGraphType> {Name = "argThree"}
                });

            const string expected = @"
type Query {
  singleField(argOne: Int, argTwo: String = ""foo"", argThree: Boolean): String
}
";
            AssertEqual(result, expected);
        }

        [Fact]
        public void prints_string_field_with_multiple_args_third_has_default()
        {
            var result = printSingleFieldSchema<StringGraphType>(
                new QueryArgument[]
                {
                    new QueryArgument<IntGraphType> {Name = "argOne"},
                    new QueryArgument<StringGraphType> {Name = "argTwo"},
                    new QueryArgument<BooleanGraphType> {Name = "argThree", DefaultValue = false}
                });

            const string expected = @"
type Query {
  singleField(argOne: Int, argTwo: String, argThree: Boolean = false): String
}
";
            AssertEqual(result, expected);
        }

        [Fact]
        public void prints_interface()
        {
            var root = new ObjectGraphType { Name = "Root" };
            root.Field<BarType>("bar");

            var schema = new Schema { Query = root };

            AssertEqual(print(schema), @"
schema {
  query: Root
}

type Bar implements Foo {
  str: String
}

# The `Date` scalar type represents a timestamp provided in UTC. `Date` expects
# timestamps to be formatted in accordance with the
# [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.
scalar Date

scalar Decimal

interface Foo {
  str: String
}

type Root {
  bar: Bar
}
", excludeScalars: true);
        }

        [Fact]
        public void prints_multiple_interfaces()
        {
            var root = new ObjectGraphType { Name = "Query" };
            root.Field<BarMultipleType>("bar");

            var schema = new Schema { Query = root };

            AssertEqual(print(schema), @"
interface Baaz {
  int: Int
}

type Bar implements Foo, Baaz {
  str: String
}

# The `Date` scalar type represents a timestamp provided in UTC. `Date` expects
# timestamps to be formatted in accordance with the
# [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.
scalar Date

scalar Decimal

interface Foo {
  str: String
}

type Query {
  bar: Bar
}
", excludeScalars: true);
        }

        [Fact]
        public void prints_unions()
        {
            var root = new ObjectGraphType { Name = "Query" };
            root.Field<SingleUnion>("single");
            root.Field<MultipleUnion>("multiple");

            var schema = new Schema { Query = root };

            AssertEqual(print(schema), @"
type Bar implements Foo {
  str: String
}

# The `Date` scalar type represents a timestamp provided in UTC. `Date` expects
# timestamps to be formatted in accordance with the
# [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.
scalar Date

scalar Decimal

interface Foo {
  str: String
}

union MultipleUnion = Foo | Bar

type Query {
  single: SingleUnion
  multiple: MultipleUnion
}

union SingleUnion = Foo
", excludeScalars: true);
        }

        [Fact]
        public void prints_input_type()
        {
            var root = new ObjectGraphType { Name = "Query" };
            root.Field<NonNullGraphType<StringGraphType>>(
                "str",
                arguments: new QueryArguments(new QueryArgument<InputType> {Name = "argOne"}));

            var schema = new Schema { Query = root };

            AssertEqual(print(schema), @"
input InputType {
  int: Int
}

type Query {
  str(argOne: InputType): String!
}
");
        }

        [Fact]
        public void prints_custom_scalar()
        {
            var root = new ObjectGraphType { Name = "Query" };
            root.Field<OddType>("odd");

            var schema = new Schema { Query = root };

            AssertEqual(print(schema), @"
scalar Odd

type Query {
  odd: Odd
}
");
        }

        [Fact]
        public void prints_enum()
        {
            var root = new ObjectGraphType { Name = "Query" };
            root.Field<RgbEnum>("rgb");

            var schema = new Schema { Query = root };

            AssertEqual(print(schema), @"
type Query {
  rgb: RGB
}

enum RGB {
  RED
  GREEN
  BLUE
}
");
        }

        [Fact]
        public void prints_introspection_schema()
        {
            var schema = new Schema
            {
                Query = new ObjectGraphType
                {
                    Name = "Root"
                }
            };
            var printer = new SchemaPrinter(schema);
            var result = Environment.NewLine + printer.PrintIntrospectionSchema();

            const string expected = @"
schema {
  query: Root
}

# Directs the executor to include this field or fragment only when the 'if' argument is true.
directive @include(
  if: Boolean!
) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT

# Directs the executor to skip this field or fragment when the 'if' argument is true.
directive @skip(
  if: Boolean!
) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT

# Marks an element of a GraphQL schema as no longer supported.
directive @deprecated(
  reason: String = ""No longer supported""
) on FIELD_DEFINITION | ENUM_VALUE

# A Directive provides a way to describe alternate runtime execution and type validation behavior in a GraphQL document.
#
# In some cases, you need to provide options to alter GraphQL's execution behavior
# in ways field arguments will not suffice, such as conditionally including or
# skipping a field. Directives provide this by describing additional information
# to the executor.
type __Directive {
  name: String!
  description: String
  locations: [__DirectiveLocation!]!
  args: [__InputValue!]!
  onOperation: Boolean!
  onFragment: Boolean!
  onField: Boolean!
}

# A Directive can be adjacent to many parts of the GraphQL language, a
# __DirectiveLocation describes one such possible adjacencies.
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

# One possible value for a given Enum. Enum values are unique values, not a
# placeholder for a string or numeric value. However an Enum value is returned in
# a JSON response as a string.
type __EnumValue {
  name: String!
  description: String
  isDeprecated: String!
  deprecationReason: String
}

# Object and Interface types are described by a list of Fields, each of which has
# a name, potentially a list of arguments, and a return type.
type __Field {
  name: String!
  description: String
  args: [__InputValue!]!
  type: __Type!
  isDeprecated: Boolean!
  deprecationReason: String
}

# Arguments provided to Fields or Directives and the input fields of an
# InputObject are represented as Input Values which describe their type and
# optionally a default value.
type __InputValue {
  name: String!
  description: String
  type: __Type!
  defaultValue: String
}

# A GraphQL Schema defines the capabilities of a GraphQL server. It exposes all
# available types and directives on the server, as well as the entry points for
# query, mutation, and subscription operations.
type __Schema {
  types: [__Type!]!
  queryType: __Type!
  mutationType: __Type
  subscriptionType: __Type
  directives: [__Directive!]!
}

# The fundamental unit of any GraphQL Schema is the type. There are many kinds of
# types in GraphQL as represented by the `__TypeKind` enum.
#
# Depending on the kind of a type, certain fields describe information about that
# type. Scalar types provide no information beyond a name and description, while
# Enum types provide their values. Object and Interface types provide the fields
# they describe. Abstract types, Union and Interface, provide the Object types
# possible at runtime. List and NonNull types compose other types.
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
}

# An enum describing what kind of type a given __Type is.
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
";

            AssertEqual(result, expected, excludeScalars: true);
        }

        public class FooType : ObjectGraphType
        {
            public FooType()
            {
                Name = "Foo";
                Field<StringGraphType>("str");
            }
        }

        public class FooInterfaceType : InterfaceGraphType
        {
            public FooInterfaceType()
            {
                Name = "Foo";
                ResolveType = obj => null;
                Field<StringGraphType>("str");
            }
        }

        public class BaazInterfaceType : InterfaceGraphType
        {
            public BaazInterfaceType()
            {
                Name = "Baaz";
                ResolveType = obj => null;
                Field<IntGraphType>("int");
            }
        }

        public class BarType : ObjectGraphType
        {
            public BarType()
            {
                Name = "Bar";
                Field<StringGraphType>("str");
                Interface<FooInterfaceType>();
            }
        }

        public class BarMultipleType : ObjectGraphType
        {
            public BarMultipleType()
            {
                Name = "Bar";
                Field<StringGraphType>("str");
                Interface<FooInterfaceType>();
                Interface<BaazInterfaceType>();
            }
        }

        public class SingleUnion : UnionGraphType
        {
            public SingleUnion()
            {
                Name = "SingleUnion";
                ResolveType = obj => null;
                Type<FooType>();
            }
        }

        public class MultipleUnion : UnionGraphType
        {
            public MultipleUnion()
            {
                Name = "MultipleUnion";
                ResolveType = obj => null;
                Type<FooType>();
                Type<BarType>();
            }
        }

        public class InputType : InputObjectGraphType
        {
            public InputType()
            {
                Name = "InputType";
                Field<IntGraphType>("int");
            }
        }

        public class OddType : ScalarGraphType
        {
            public OddType()
            {
                Name = "Odd";
            }

            public override object Serialize(object value)
            {
                return null;
            }

            public override object ParseValue(object value)
            {
                return null;
            }

            public override object ParseLiteral(IValue value)
            {
                return null;
            }
        }

        public class RgbEnum : EnumerationGraphType
        {
            public RgbEnum()
            {
                Name = "RGB";
                AddValue("RED", "", 0);
                AddValue("GREEN", "", 1);
                AddValue("BLUE", "", 2);
            }
        }
    }
}
