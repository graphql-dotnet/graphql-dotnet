using System;
using System.Collections.Generic;
using GraphQL.Language;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;
using Should;

namespace GraphQL.Tests.Utilities
{
    public class SchemaPrinterTests
    {
        private string printSingleFieldSchema<T>(
            IEnumerable<QueryArgument> arguments = null)
            where T : GraphType
        {
            var args = arguments != null ? new QueryArguments(arguments) : null;

            var root = new ObjectGraphType();
            root.Name = "Root";
            root.Field<T>(
                "singleField",
                arguments: args);

            var schema = new Schema
            {
                Query = root
            };

            return print(schema);
        }

        private string print(ISchema schema)
        {
            var printer = new SchemaPrinter(schema);
            return Environment.NewLine + printer.Print();
        }

        private void AssertEqual(string result, string expected)
        {
            result.Replace("\r", "").ShouldEqual(expected.Replace("\r", ""));
        }

        [Fact]
        public void prints_string_field()
        {
            var result = printSingleFieldSchema<StringGraphType>();
            const string expected = @"
type Root {
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
type Root {
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
type Root {
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
type Root {
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
type Root {
  singleField: [String!]!
}
";
            AssertEqual(result, expected);
        }

        [Fact]
        public void prints_object_field()
        {
            var root = new ObjectGraphType {Name = "Root"};
            root.Field<FooType>("foo");

            var schema = new Schema {Query = root};

            AssertEqual(print(schema), @"
type Foo {
  str: String
}

type Root {
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
type Root {
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
type Root {
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
type Root {
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
type Root {
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
type Root {
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
type Root {
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
type Root {
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
interface Foo {
  str: String
}

type Bar implements Foo {
  str: String
}

type Root {
  bar: Bar
}
");
        }

        [Fact]
        public void prints_multiple_interfaces()
        {
            var root = new ObjectGraphType { Name = "Root" };
            root.Field<BarMultipleType>("bar");

            var schema = new Schema { Query = root };

            AssertEqual(print(schema), @"
interface Baaz {
  int: Int
}

interface Foo {
  str: String
}

type Bar implements Foo, Baaz {
  str: String
}

type Root {
  bar: Bar
}
");
        }

        [Fact]
        public void prints_unions()
        {
            var root = new ObjectGraphType { Name = "Root" };
            root.Field<SingleUnion>("single");
            root.Field<MultipleUnion>("multiple");

            var schema = new Schema { Query = root };

            AssertEqual(print(schema), @"
interface Foo {
  str: String
}

type Bar implements Foo {
  str: String
}

type Root {
  single: SingleUnion
  multiple: MultipleUnion
}

union MultipleUnion = Foo | Bar

union SingleUnion = Foo
");
        }

        [Fact]
        public void prints_input_type()
        {
            var root = new ObjectGraphType { Name = "Root" };
            root.Field<StringGraphType>(
                "str",
                arguments: new QueryArguments(new QueryArgument[]
                {
                    new QueryArgument<InputType> {Name = "argOne"},
                }));

            var schema = new Schema { Query = root };

            AssertEqual(print(schema), @"
input InputType {
  int: Int
}

type Root {
  str(argOne: InputType): String
}
");
        }

        [Fact]
        public void prints_custom_scalar()
        {
            var root = new ObjectGraphType { Name = "Root" };
            root.Field<OddType>("odd");

            var schema = new Schema { Query = root };

            AssertEqual(print(schema), @"
scalar Odd

type Root {
  odd: Odd
}
");
        }

        [Fact]
        public void prints_enum()
        {
            var root = new ObjectGraphType { Name = "Root" };
            root.Field<RgbEnum>("rgb");

            var schema = new Schema { Query = root };

            AssertEqual(print(schema), @"
enum RGB {
  RED
  GREEN
  BLUE
}

type Root {
  rgb: RGB
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
  args: [__InputValue!]!
  onOperation: Boolean!
  onFragment: Boolean!
  onField: Boolean!
}

type __EnumValue {
  name: String!
  description: String
  isDeprecated: String!
  deprecationReason: String
}

type __Field {
  name: String!
  description: String
  args: [__InputValue!]!
  type: __Type!
  isDeprecated: Boolean!
  deprecationReason: String
}

type __InputValue {
  name: String!
  description: String
  type: __Type!
  defaultValue: String
}

type __Schema {
  types: [__Type!]!
  queryType: __Type!
  mutationType: __Type
  directives: [__Directive!]!
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
}
";

            AssertEqual(result, expected);
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
