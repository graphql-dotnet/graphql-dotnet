using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser.AST;

namespace GraphQL.Tests.Utilities;

public class SchemaPrinterTests
{
    private string printSingleFieldSchema<T>(
        IEnumerable<QueryArgument> arguments = null)
        where T : GraphType
    {
        var args = arguments != null ? new QueryArguments(arguments) : null;

        var root = new ObjectGraphType { Name = "Query" };
        root.Field<T>(
            "singleField",
            arguments: args);

        var schema = new Schema
        {
            Query = root
        };

        var result = print(schema);

        // ensure schema isn't disposed before test finishes
        schema.Query.Name.ShouldNotBeNull();

        return result;
    }

    private static string print(ISchema schema)
    {
        return print(schema, new SchemaPrinterOptions { IncludeDescriptions = true, IncludeDeprecationReasons = true, PrintDescriptionsAsComments = true });
    }

    private static string print(ISchema schema, SchemaPrinterOptions options)
    {
        var printer = new SchemaPrinter(schema, options);
        return Environment.NewLine + printer.Print();
    }

    private void AssertEqual(string result, string expectedName, string expected, bool excludeScalars = false)
    {
        AssertEqual(
            result,
            new Dictionary<string, string> { { expectedName, expected } },
            excludeScalars);
    }

    private void AssertEqual(string result, Dictionary<string, string> expected, bool excludeScalars = false)
    {
        string exp;
        if (excludeScalars)
        {
            exp = string.Join($"{Environment.NewLine}{Environment.NewLine}", expected
                .OrderBy(x => x.Key)
                .Select(x => x.Value));
        }
        else
        {
            var orderedScalars = expected
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => x.Value);
            exp = Environment.NewLine + string.Join($"{Environment.NewLine}{Environment.NewLine}", orderedScalars) + Environment.NewLine;
        }

        result.Replace("\r", "").ShouldBe(exp.Replace("\r", ""));
    }

    private class TestSchemaTypes : SchemaTypes
    {
    }

    [Fact]
    public void prints_directive()
    {
        var printer = new SchemaPrinter(null, new SchemaPrinterOptions { IncludeDescriptions = true, PrintDescriptionsAsComments = true });
        var skip = new SkipDirective();
        var arg = skip.Arguments.First();
        arg.ResolvedType = new TestSchemaTypes().BuildGraphQLType(arg.Type, null);

        var result = printer.PrintDirective(skip);
        const string expected = @"# Directs the executor to skip this field or fragment when the 'if' argument is true.
directive @skip(
  if: Boolean!
) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT";

        AssertEqual(result, "directive", expected, excludeScalars: true);
    }

    [Fact]
    public void prints_directive_2()
    {
        var printer = new SchemaPrinter(null, new SchemaPrinterOptions { IncludeDescriptions = true, PrintDescriptionsAsComments = false });
        var skip = new SkipDirective();
        var arg = skip.Arguments.First();
        arg.ResolvedType = new TestSchemaTypes().BuildGraphQLType(arg.Type, null);

        var result = printer.PrintDirective(skip);
        const string expected = @"""""""
Directs the executor to skip this field or fragment when the 'if' argument is true.
""""""
directive @skip(
  if: Boolean!
) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT";

        AssertEqual(result, "directive", expected, excludeScalars: true);
    }

    [Fact]
    public void prints_directive_without_arguments()
    {
        var d = new Directive("my", DirectiveLocation.Field, DirectiveLocation.Query);
        string result = new SchemaPrinter(null).PrintDirective(d);
        result.ShouldBe("directive @my on FIELD | QUERY");
    }

    [Fact]
    public void prints_repeatable_directive_without_arguments()
    {
        var d = new Directive("my", DirectiveLocation.Field, DirectiveLocation.Query) { Repeatable = true };
        string result = new SchemaPrinter(null).PrintDirective(d);
        result.ShouldBe("directive @my repeatable on FIELD | QUERY");
    }

    [Fact]
    public void prints_repeatable_directive_with_arguments()
    {
        var d = new Directive("my", DirectiveLocation.Field, DirectiveLocation.Query)
        {
            Repeatable = true,
            Arguments = new QueryArguments(new QueryArgument(new IntGraphType()) { Name = "max" })
        };
        string result = new SchemaPrinter(null).PrintDirective(d);
        result.ShouldBe(@"directive @my(
  max: Int
) repeatable on FIELD | QUERY");
    }

    [Fact]
    public void prints_string_field()
    {
        var result = printSingleFieldSchema<StringGraphType>();
        const string expected =
@"type Query {
  singleField: String
}";
        AssertEqual(result, "Query", expected);
    }

    [Fact]
    public void prints_string_list_field()
    {
        var result = printSingleFieldSchema<ListGraphType<StringGraphType>>();
        const string expected =
@"type Query {
  singleField: [String]
}";
        AssertEqual(result, "Query", expected);
    }

    [Fact]
    public void prints_non_null_string_field()
    {
        var result = printSingleFieldSchema<NonNullGraphType<StringGraphType>>();
        const string expected =
@"type Query {
  singleField: String!
}";
        AssertEqual(result, "Query", expected);
    }

    [Fact]
    public void prints_non_null_list_of_string_field()
    {
        var result = printSingleFieldSchema<NonNullGraphType<ListGraphType<StringGraphType>>>();
        const string expected =
@"type Query {
  singleField: [String]!
}";
        AssertEqual(result, "Query", expected);
    }

    [Fact]
    public void prints_non_null_list_of_non_null_string_field()
    {
        var result = printSingleFieldSchema<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>();
        const string expected =
@"type Query {
  singleField: [String!]!
}";
        AssertEqual(result, "Query", expected);
    }

    [Fact]
    public void prints_object_field()
    {
        var root = new ObjectGraphType { Name = "Query" };
        root.Field<FooType>("foo");

        var schema = new Schema { Query = root };

        var expected = new Dictionary<string, string>
        {
            {
                "Foo",
@"# This is a Foo object type
type Foo {
  # This is of type String
  str: String
  # This is of type Integer
  int: Int @deprecated(reason: ""This field is now deprecated"")
}"
            },
            {
                "Query",
@"type Query {
  foo: Foo
}"
            },
        };
        AssertEqual(print(schema), expected);
    }

    [Fact]
    public void prints_object_field_with_field_descriptions()
    {
        var root = new ObjectGraphType { Name = "Query" };
        root.Field<FooType>("foo");

        var schema = new Schema { Query = root };

        var options = new SchemaPrinterOptions
        {
            IncludeDescriptions = true,
            PrintDescriptionsAsComments = true,
        };

        var expected = new Dictionary<string, string>
        {
            {
                "Foo",
@"# This is a Foo object type
type Foo {
  # This is of type String
  str: String
  # This is of type Integer
  int: Int
}"
            },
            {
                "Query",
@"type Query {
  foo: Foo
}"
            },
        };
        AssertEqual(print(schema, options), expected);
    }

    [Fact]
    public void prints_object_field_with_field_descriptions_2()
    {
        var root = new ObjectGraphType { Name = "Query" };
        root.Field<FooType>("foo");

        var schema = new Schema { Query = root };

        var options = new SchemaPrinterOptions
        {
            IncludeDescriptions = true,
            PrintDescriptionsAsComments = false,
        };

        var expected = new Dictionary<string, string>
        {
            {
                "Foo",
@"""""""
This is a Foo object type
""""""
type Foo {
  """"""
  This is of type String
  """"""
  str: String
  """"""
  This is of type Integer
  """"""
  int: Int
}"
            },
            {
                "Query",
@"type Query {
  foo: Foo
}"
            },
        };
        AssertEqual(print(schema, options), expected);
    }

    [Fact]
    public void prints_object_field_with_field_descriptions_and_deprecation_reasons()
    {
        var root = new ObjectGraphType { Name = "Query" };
        root.Field<FooType>("foo");

        var schema = new Schema { Query = root };

        var options = new SchemaPrinterOptions
        {
            IncludeDescriptions = true,
            IncludeDeprecationReasons = true,
            PrintDescriptionsAsComments = true,
        };

        var expected = new Dictionary<string, string>
        {
            {
                "Foo",
@"# This is a Foo object type
type Foo {
  # This is of type String
  str: String
  # This is of type Integer
  int: Int
}".Replace("int: Int", "int: Int @deprecated(reason: \"This field is now deprecated\")")
            },
            {
                "Query",
@"type Query {
  foo: Foo
}"
            },
        };
        var result = print(schema, options);
        AssertEqual(result, expected);
    }

    [Fact]
    public void prints_object_field_with_field_descriptions_and_deprecation_reasons_2()
    {
        var root = new ObjectGraphType { Name = "Query" };
        root.Field<FooType>("foo");

        var schema = new Schema { Query = root };

        var options = new SchemaPrinterOptions
        {
            IncludeDescriptions = true,
            IncludeDeprecationReasons = true,
            PrintDescriptionsAsComments = false,
        };

        var expected = new Dictionary<string, string>
        {
            {
                "Foo",
@"""""""
This is a Foo object type
""""""
type Foo {
  """"""
  This is of type String
  """"""
  str: String
  """"""
  This is of type Integer
  """"""
  int: Int
}".Replace("int: Int", "int: Int @deprecated(reason: \"This field is now deprecated\")")
            },
            {
                "Query",
@"type Query {
  foo: Foo
}"
            },
        };
        var result = print(schema, options);
        AssertEqual(result, expected);
    }

    [Fact]
    public void prints_string_field_with_int_arg()
    {
        var result = printSingleFieldSchema<StringGraphType>(
            new[]
            {
                new QueryArgument<IntGraphType> { Name = "argOne" }
            });

        const string expected =
@"type Query {
  singleField(argOne: Int): String
}";
        AssertEqual(result, "Query", expected);
    }

    [Fact]
    public void prints_string_field_with_int_arg_with_default()
    {
        var result = printSingleFieldSchema<StringGraphType>(
            new[]
            {
                new QueryArgument<IntGraphType> { Name = "argOne", DefaultValue = 2 }
            });

        const string expected =
@"type Query {
  singleField(argOne: Int = 2): String
}";
        AssertEqual(result, "Query", expected);
    }

    [Fact]
    public void prints_string_field_with_non_null_int_arg()
    {
        var result = printSingleFieldSchema<StringGraphType>(
            new[]
            {
                new QueryArgument<NonNullGraphType<IntGraphType>> { Name = "argOne" }
            });

        const string expected =
@"type Query {
  singleField(argOne: Int!): String
}";
        AssertEqual(result, "Query", expected);
    }

    [Fact]
    public void prints_string_field_with_multiple_args()
    {
        var result = printSingleFieldSchema<StringGraphType>(
            new QueryArgument[]
            {
                new QueryArgument<IntGraphType> { Name = "argOne" },
                new QueryArgument<StringGraphType> { Name = "argTwo" }
            });

        const string expected =
@"type Query {
  singleField(argOne: Int, argTwo: String): String
}";
        AssertEqual(result, "Query", expected);
    }

    [Fact]
    public void prints_string_field_with_multiple_args_first_has_default()
    {
        var result = printSingleFieldSchema<StringGraphType>(
            new QueryArgument[]
            {
                new QueryArgument<IntGraphType> { Name = "argOne", DefaultValue = 1 },
                new QueryArgument<StringGraphType> { Name = "argTwo" },
                new QueryArgument<BooleanGraphType> { Name = "argThree" }
            });

        const string expected =
@"type Query {
  singleField(argOne: Int = 1, argTwo: String, argThree: Boolean): String
}";
        AssertEqual(result, "Query", expected);
    }

    [Fact]
    public void prints_string_field_with_multiple_args_second_has_default()
    {
        var result = printSingleFieldSchema<StringGraphType>(
            new QueryArgument[]
            {
                new QueryArgument<IntGraphType> { Name = "argOne" },
                new QueryArgument<StringGraphType> { Name = "argTwo", DefaultValue = "foo" },
                new QueryArgument<BooleanGraphType> { Name = "argThree" }
            });

        const string expected =
@"type Query {
  singleField(argOne: Int, argTwo: String = ""foo"", argThree: Boolean): String
}";
        AssertEqual(result, "Query", expected);
    }

    [Fact]
    public void prints_string_field_with_multiple_args_third_has_default()
    {
        var result = printSingleFieldSchema<StringGraphType>(
            new QueryArgument[]
            {
                new QueryArgument<IntGraphType> { Name = "argOne" },
                new QueryArgument<StringGraphType> { Name = "argTwo" },
                new QueryArgument<BooleanGraphType> { Name = "argThree", DefaultValue = false }
            });

        const string expected =
@"type Query {
  singleField(argOne: Int, argTwo: String, argThree: Boolean = false): String
}";
        AssertEqual(result, "Query", expected);
    }

    [Fact]
    public void prints_interface()
    {
        var root = new ObjectGraphType { Name = "Root" };
        root.Field<BarType>("bar");

        var schema = new Schema { Query = root };

        AssertEqual(print(schema), "", @"
schema {
  query: Root
}

type Bar implements IFoo {
  # This is of type String
  str: String
}

# This is a Foo interface type
interface IFoo {
  # This is of type String
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

        var result = print(schema);

        AssertEqual(result, "", @"
interface Baaz {
  # This is of type Integer
  int: Int
}

type Bar implements IFoo & Baaz {
  # This is of type String
  str: String
  # This is of type Integer
  int: Int
}

# This is a Foo interface type
interface IFoo {
  # This is of type String
  str: String
}

type Query {
  bar: Bar
}
", excludeScalars: true);
    }

    [Fact]
    public void prints_multiple_interfaces_with_old_implements_syntax()
    {
        var root = new ObjectGraphType { Name = "Query" };
        root.Field<BarMultipleType>("bar");

        var schema = new Schema { Query = root };

        var options = new SchemaPrinterOptions
        {
            OldImplementsSyntax = true,
            IncludeDescriptions = true,
            PrintDescriptionsAsComments = true,
        };

        AssertEqual(print(schema, options), "", @"
interface Baaz {
  # This is of type Integer
  int: Int
}

type Bar implements IFoo, Baaz {
  # This is of type String
  str: String
  # This is of type Integer
  int: Int
}

# This is a Foo interface type
interface IFoo {
  # This is of type String
  str: String
}

type Query {
  bar: Bar
}
", excludeScalars: true);
    }

    [Fact]
    public void prints_multiple_interfaces_with_old_implements_syntax_2()
    {
        var root = new ObjectGraphType { Name = "Query" };
        root.Field<BarMultipleType>("bar");

        var schema = new Schema { Query = root };

        var options = new SchemaPrinterOptions
        {
            OldImplementsSyntax = true,
            IncludeDescriptions = true,
            PrintDescriptionsAsComments = false,
        };

        AssertEqual(print(schema, options), "", @"
interface Baaz {
  """"""
  This is of type Integer
  """"""
  int: Int
}

type Bar implements IFoo, Baaz {
  """"""
  This is of type String
  """"""
  str: String
  """"""
  This is of type Integer
  """"""
  int: Int
}

""""""
This is a Foo interface type
""""""
interface IFoo {
  """"""
  This is of type String
  """"""
  str: String
}

type Query {
  bar: Bar
}
", excludeScalars: true);
    }

    [Fact]
    public void prints_multiple_interfaces_with_field_descriptions()
    {
        var root = new ObjectGraphType { Name = "Query" };
        root.Field<BarMultipleType>("bar");

        var schema = new Schema { Query = root };

        var options = new SchemaPrinterOptions
        {
            IncludeDescriptions = true,
            PrintDescriptionsAsComments = true,
        };

        var result = print(schema, options);

        AssertEqual(result, "", @"
interface Baaz {
  # This is of type Integer
  int: Int
}

type Bar implements IFoo & Baaz {
  # This is of type String
  str: String
  # This is of type Integer
  int: Int
}

# This is a Foo interface type
interface IFoo {
  # This is of type String
  str: String
}

type Query {
  bar: Bar
}
", excludeScalars: true);
    }

    [Fact]
    public void prints_multiple_interfaces_with_field_descriptions_2()
    {
        var root = new ObjectGraphType { Name = "Query" };
        root.Field<BarMultipleType>("bar");

        var schema = new Schema { Query = root };

        var options = new SchemaPrinterOptions
        {
            IncludeDescriptions = true,
            PrintDescriptionsAsComments = false,
        };

        var result = print(schema, options);

        AssertEqual(result, "", @"
interface Baaz {
  """"""
  This is of type Integer
  """"""
  int: Int
}

type Bar implements IFoo & Baaz {
  """"""
  This is of type String
  """"""
  str: String
  """"""
  This is of type Integer
  """"""
  int: Int
}

""""""
This is a Foo interface type
""""""
interface IFoo {
  """"""
  This is of type String
  """"""
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

        AssertEqual(print(schema), "", @"
type Bar implements IFoo {
  # This is of type String
  str: String
}

# This is a Foo object type
type Foo {
  # This is of type String
  str: String
  # This is of type Integer
  int: Int @deprecated(reason: ""This field is now deprecated"")
}

# This is a Foo interface type
interface IFoo {
  # This is of type String
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
            arguments: new QueryArguments(new QueryArgument<InputType> { Name = "argOne" }));

        var schema = new Schema { Query = root };

        var expected = new Dictionary<string, string>
        {
            {
                "InputType",
@"input InputType {
  int: Int
}"
            },
                            {
                "Query",
@"type Query {
  str(argOne: InputType): String!
}"
            },
        };
        AssertEqual(print(schema), expected);
    }

    [Fact]
    public void prints_input_type_with_default()
    {
        var root = new ObjectGraphType { Name = "Query" };
        root.Field<NonNullGraphType<StringGraphType>>(
            "str",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<SomeInputType>> { Name = "argOne", DefaultValue = new SomeInput { Name = "Tom", Age = 42, IsDeveloper = true } },
                new QueryArgument<ListGraphType<SomeInputType>> { Name = "argTwo", DefaultValue = new[] { new SomeInput { Name = "Tom1", Age = 12 }, new SomeInput { Name = "Tom2", Age = 22, IsDeveloper = true } } })
            );

        var schema = new Schema { Query = root };

        var expected = new Dictionary<string, string>
        {
            {
                "SomeInput",
@"input SomeInput {
  age: Int!
  name: String!
  isDeveloper: Boolean!
}"
            },
                            {
                "Query",
@"type Query {
  str(argOne: SomeInput! = { age: 42, name: ""Tom"", isDeveloper: true }, argTwo: [SomeInput] = [{ age: 12, name: ""Tom1"", isDeveloper: false }, { age: 22, name: ""Tom2"", isDeveloper: true }]): String!
}"
            },
        };
        AssertEqual(print(schema), expected);
    }

    [Fact]
    public void prints_input_type_with_default_null_value()
    {
        var root = new ObjectGraphType { Name = "Query" };
        root.Field<NonNullGraphType<StringGraphType>>(
            "str",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<SomeInputType2>> { Name = "argOne", DefaultValue = new SomeInput2 { Names = null } })
            );

        var schema = new Schema { Query = root };

        var expected = new Dictionary<string, string>
        {
            {
                "SomeInput2",
@"input SomeInput2 {
  names: [String]
}"
            },
                            {
                "Query",
@"type Query {
  str(argOne: SomeInput2! = { names: null }): String!
}"
            },
        };
        AssertEqual(print(schema), expected);
    }

    [Fact]
    public void prints_input_type_with_default_as_dictionary()
    {
        var schema = Schema.For(@"
input SomeInput {
  age: Int!
  name: String!
  isDeveloper: Boolean!
  unused: Boolean
}

type Query {
  str(argOne: SomeInput! = { age: 42, name: ""Tom"", isDeveloper: true },
      argTwo: [SomeInput] = [{ age: 12, name: ""Tom1"", isDeveloper: false }, { age: 22, name: ""Tom2"", isDeveloper: true }]): String!
}
");

        var expected = new Dictionary<string, string>
        {
            {
                "SomeInput",
@"input SomeInput {
  age: Int!
  name: String!
  isDeveloper: Boolean!
  unused: Boolean
}"
            },
                            {
                "Query",
@"type Query {
  str(argOne: SomeInput! = { age: 42, name: ""Tom"", isDeveloper: true }, argTwo: [SomeInput] = [{ age: 12, name: ""Tom1"", isDeveloper: false }, { age: 22, name: ""Tom2"", isDeveloper: true }]): String!
}"
            },
        };
        AssertEqual(print(schema), expected);
    }

    [Fact]
    public void prints_input_type_with_default_as_dictionary_null_values()
    {
        var schema = Schema.For(@"
input SomeInput {
  age: Int = 2
}

type Query {
  str(arg: SomeInput = { age: null }): String!
}
");

        var expected = new Dictionary<string, string>
        {
            {
                "SomeInput",
@"input SomeInput {
  age: Int = 2
}"
            },
                            {
                "Query",
@"type Query {
  str(arg: SomeInput = { age: null }): String!
}"
            },
        };
        AssertEqual(print(schema), expected);
    }

    [Fact]
    public void prints_custom_scalar()
    {
        var root = new ObjectGraphType { Name = "Query" };
        root.Field<OddType>("odd");

        var schema = new Schema { Query = root };

        var expected = new Dictionary<string, string>
        {
            { "Odd", @"scalar Odd" },
            {
                "Query",
@"type Query {
  odd: Odd
}"
            },
        };
        AssertEqual(print(schema), expected);
    }

    [Fact]
    public void prints_builtin_scalars()
    {
        var root = new ObjectGraphType { Name = "Query" };
        root.Field<BigIntGraphType>("bigint");
        root.Field<ByteGraphType>("byte");
        root.Field<DateGraphType>("date");
        root.Field<DateTimeGraphType>("datetime");
        root.Field<DateTimeOffsetGraphType>("datetimeoffset");
        root.Field<DecimalGraphType>("decimal");
        root.Field<GuidGraphType>("guid");
        root.Field<LongGraphType>("long");
        root.Field<TimeSpanMillisecondsGraphType>("milliseconds");
        root.Field<SByteGraphType>("sbyte");
        root.Field<TimeSpanSecondsGraphType>("seconds");
        root.Field<ShortGraphType>("short");
        root.Field<UIntGraphType>("uint");
        root.Field<ULongGraphType>("ulong");
        root.Field<UShortGraphType>("ushort");
        root.Field<UriGraphType>("uri");

        var schema = new Schema { Query = root };

        var expected = new Dictionary<string, string>
        {
            {
                "Query",
@"scalar BigInt

scalar Byte

# The `Date` scalar type represents a year, month and day in accordance with the
# [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.
scalar Date

# The `DateTime` scalar type represents a date and time. `DateTime` expects
# timestamps to be formatted in accordance with the
# [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.
scalar DateTime

# The `DateTimeOffset` scalar type represents a date, time and offset from UTC.
# `DateTimeOffset` expects timestamps to be formatted in accordance with the
# [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.
scalar DateTimeOffset

scalar Decimal

scalar Guid

scalar Long

# The `Milliseconds` scalar type represents a period of time represented as the
# total number of milliseconds in range [-922337203685477, 922337203685477].
scalar Milliseconds

type Query {
  bigint: BigInt
  byte: Byte
  date: Date
  datetime: DateTime
  datetimeoffset: DateTimeOffset
  decimal: Decimal
  guid: Guid
  long: Long
  milliseconds: Milliseconds
  sbyte: SByte
  seconds: Seconds
  short: Short
  uint: UInt
  ulong: ULong
  ushort: UShort
  uri: Uri
}

scalar SByte

# The `Seconds` scalar type represents a period of time represented as the total
# number of seconds in range [-922337203685, 922337203685].
scalar Seconds

scalar Short

scalar UInt

scalar ULong

scalar UShort

scalar Uri"
            },
        };
        AssertEqual(print(schema), expected);
    }

    [Fact]
    public void prints_enum()
    {
        var root = new ObjectGraphType { Name = "Query" };
        root.Field<RgbEnum>("rgb");

        var schema = new Schema { Query = root };

        var expected = new Dictionary<string, string>
        {
            {
                "Query",
@"type Query {
  rgb: RGB
}"
            },
            {
                "RGB",
@"enum RGB {
  # Red!
  RED @deprecated(reason: ""Use green!"")
  # Green!
  GREEN
  # Blue!
  BLUE
}"
            },
        };
        AssertEqual(print(schema), expected);
    }

    [Fact]
    public void prints_enum_default_args()
    {
        var root = new ObjectGraphType { Name = "Query" };

        var f = new FieldType
        {
            Name = "bestColor",
            Arguments = new QueryArguments(new QueryArgument<RgbEnum>
            {
                Name = "color",
                DefaultValue = 0 // 0 = red --- must be internal representation of enumeration value or validation will fail
            }),
            Type = typeof(RgbEnum)
        };
        root.AddField(f);
        var schema = new Schema { Query = root };
        schema.RegisterType<RgbEnum>();
        var expected = new Dictionary<string, string>
        {
            {
                "Query",
@"type Query {
  bestColor(color: RGB = RED): RGB
}"
            },
            {
                "RGB",
@"enum RGB {
  # Red!
  RED @deprecated(reason: ""Use green!"")
  # Green!
  GREEN
  # Blue!
  BLUE
}"
            },
        };
        AssertEqual(print(schema), expected);
    }

    [Fact]
    public void prints_introspection_schema_with_descriptions_as_comments()
    {
        var schema = new Schema
        {
            Query = new ObjectGraphType
            {
                Name = "Root"
            }
        };
        schema.Query.Fields.Add(new FieldType { Name = "unused", ResolvedType = new StringGraphType() });
        var printer = new SchemaPrinter(schema, new SchemaPrinterOptions { IncludeDescriptions = true, PrintDescriptionsAsComments = true });
        var result = Environment.NewLine + printer.PrintIntrospectionSchema();

        const string expected = @"
schema {
  query: Root
}

# Marks an element of a GraphQL schema as no longer supported.
directive @deprecated(
  reason: String = ""No longer supported""
) on FIELD_DEFINITION | ENUM_VALUE

# Directs the executor to include this field or fragment only when the 'if' argument is true.
directive @include(
  if: Boolean!
) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT

# Directs the executor to skip this field or fragment when the 'if' argument is true.
directive @skip(
  if: Boolean!
) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT

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
  # Location adjacent to a query operation.
  QUERY
  # Location adjacent to a mutation operation.
  MUTATION
  # Location adjacent to a subscription operation.
  SUBSCRIPTION
  # Location adjacent to a field.
  FIELD
  # Location adjacent to a fragment definition.
  FRAGMENT_DEFINITION
  # Location adjacent to a fragment spread.
  FRAGMENT_SPREAD
  # Location adjacent to an inline fragment.
  INLINE_FRAGMENT
  # Location adjacent to a variable definition.
  VARIABLE_DEFINITION
  # Location adjacent to a schema definition.
  SCHEMA
  # Location adjacent to a scalar definition.
  SCALAR
  # Location adjacent to an object type definition.
  OBJECT
  # Location adjacent to a field definition.
  FIELD_DEFINITION
  # Location adjacent to an argument definition.
  ARGUMENT_DEFINITION
  # Location adjacent to an interface definition.
  INTERFACE
  # Location adjacent to a union definition.
  UNION
  # Location adjacent to an enum definition
  ENUM
  # Location adjacent to an enum value definition
  ENUM_VALUE
  # Location adjacent to an input object type definition.
  INPUT_OBJECT
  # Location adjacent to an input object field definition.
  INPUT_FIELD_DEFINITION
}

# One possible value for a given Enum. Enum values are unique values, not a
# placeholder for a string or numeric value. However an Enum value is returned in
# a JSON response as a string.
type __EnumValue {
  name: String!
  description: String
  isDeprecated: Boolean!
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
  # A GraphQL-formatted string representing the default value for this input value.
  defaultValue: String
}

# A GraphQL Schema defines the capabilities of a GraphQL server. It exposes all
# available types and directives on the server, as well as the entry points for
# query, mutation, and subscription operations.
type __Schema {
  description: String
  # A list of all types supported by this server.
  types: [__Type!]!
  # The type that query operations will be rooted at.
  queryType: __Type!
  # If this server supports mutation, the type that mutation operations will be rooted at.
  mutationType: __Type
  # If this server supports subscription, the type that subscription operations will be rooted at.
  subscriptionType: __Type
  # A list of all directives supported by this server.
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
  # Indicates this type is a scalar.
  SCALAR
  # Indicates this type is an object. `fields` and `possibleTypes` are valid fields.
  OBJECT
  # Indicates this type is an interface. `fields` and `possibleTypes` are valid fields.
  INTERFACE
  # Indicates this type is a union. `possibleTypes` is a valid field.
  UNION
  # Indicates this type is an enum. `enumValues` is a valid field.
  ENUM
  # Indicates this type is an input object. `inputFields` is a valid field.
  INPUT_OBJECT
  # Indicates this type is a list. `ofType` is a valid field.
  LIST
  # Indicates this type is a non-null. `ofType` is a valid field.
  NON_NULL
}
";

        AssertEqual(result, "", expected, excludeScalars: true);
    }
    [Fact]
    public void prints_introspection_schema_with_descriptions()
    {
        var schema = new Schema
        {
            Query = new ObjectGraphType
            {
                Name = "Root"
            }
        };
        schema.Query.Fields.Add(new FieldType { Name = "unused", ResolvedType = new StringGraphType() });
        var printer = new SchemaPrinter(schema, new SchemaPrinterOptions { IncludeDescriptions = true, PrintDescriptionsAsComments = false });
        var result = Environment.NewLine + printer.PrintIntrospectionSchema();

        const string expected = @"
schema {
  query: Root
}

""""""
Marks an element of a GraphQL schema as no longer supported.
""""""
directive @deprecated(
  reason: String = ""No longer supported""
) on FIELD_DEFINITION | ENUM_VALUE

""""""
Directs the executor to include this field or fragment only when the 'if' argument is true.
""""""
directive @include(
  if: Boolean!
) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT

""""""
Directs the executor to skip this field or fragment when the 'if' argument is true.
""""""
directive @skip(
  if: Boolean!
) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT

""""""
A Directive provides a way to describe alternate runtime execution and type validation behavior in a GraphQL document.

In some cases, you need to provide options to alter GraphQL's execution behavior in ways field arguments will not suffice, such as conditionally including or skipping a field. Directives provide this by describing additional information to the executor.
""""""
type __Directive {
  name: String!
  description: String
  locations: [__DirectiveLocation!]!
  args: [__InputValue!]!
  onOperation: Boolean!
  onFragment: Boolean!
  onField: Boolean!
}

""""""
A Directive can be adjacent to many parts of the GraphQL language, a __DirectiveLocation describes one such possible adjacencies.
""""""
enum __DirectiveLocation {
  """"""
  Location adjacent to a query operation.
  """"""
  QUERY
  """"""
  Location adjacent to a mutation operation.
  """"""
  MUTATION
  """"""
  Location adjacent to a subscription operation.
  """"""
  SUBSCRIPTION
  """"""
  Location adjacent to a field.
  """"""
  FIELD
  """"""
  Location adjacent to a fragment definition.
  """"""
  FRAGMENT_DEFINITION
  """"""
  Location adjacent to a fragment spread.
  """"""
  FRAGMENT_SPREAD
  """"""
  Location adjacent to an inline fragment.
  """"""
  INLINE_FRAGMENT
  """"""
  Location adjacent to a variable definition.
  """"""
  VARIABLE_DEFINITION
  """"""
  Location adjacent to a schema definition.
  """"""
  SCHEMA
  """"""
  Location adjacent to a scalar definition.
  """"""
  SCALAR
  """"""
  Location adjacent to an object type definition.
  """"""
  OBJECT
  """"""
  Location adjacent to a field definition.
  """"""
  FIELD_DEFINITION
  """"""
  Location adjacent to an argument definition.
  """"""
  ARGUMENT_DEFINITION
  """"""
  Location adjacent to an interface definition.
  """"""
  INTERFACE
  """"""
  Location adjacent to a union definition.
  """"""
  UNION
  """"""
  Location adjacent to an enum definition
  """"""
  ENUM
  """"""
  Location adjacent to an enum value definition
  """"""
  ENUM_VALUE
  """"""
  Location adjacent to an input object type definition.
  """"""
  INPUT_OBJECT
  """"""
  Location adjacent to an input object field definition.
  """"""
  INPUT_FIELD_DEFINITION
}

""""""
One possible value for a given Enum. Enum values are unique values, not a placeholder for a string or numeric value. However an Enum value is returned in a JSON response as a string.
""""""
type __EnumValue {
  name: String!
  description: String
  isDeprecated: Boolean!
  deprecationReason: String
}

""""""
Object and Interface types are described by a list of Fields, each of which has a name, potentially a list of arguments, and a return type.
""""""
type __Field {
  name: String!
  description: String
  args: [__InputValue!]!
  type: __Type!
  isDeprecated: Boolean!
  deprecationReason: String
}

""""""
Arguments provided to Fields or Directives and the input fields of an InputObject are represented as Input Values which describe their type and optionally a default value.
""""""
type __InputValue {
  name: String!
  description: String
  type: __Type!
  """"""
  A GraphQL-formatted string representing the default value for this input value.
  """"""
  defaultValue: String
}

""""""
A GraphQL Schema defines the capabilities of a GraphQL server. It exposes all available types and directives on the server, as well as the entry points for query, mutation, and subscription operations.
""""""
type __Schema {
  description: String
  """"""
  A list of all types supported by this server.
  """"""
  types: [__Type!]!
  """"""
  The type that query operations will be rooted at.
  """"""
  queryType: __Type!
  """"""
  If this server supports mutation, the type that mutation operations will be rooted at.
  """"""
  mutationType: __Type
  """"""
  If this server supports subscription, the type that subscription operations will be rooted at.
  """"""
  subscriptionType: __Type
  """"""
  A list of all directives supported by this server.
  """"""
  directives: [__Directive!]!
}

""""""
The fundamental unit of any GraphQL Schema is the type. There are many kinds of types in GraphQL as represented by the `__TypeKind` enum.

Depending on the kind of a type, certain fields describe information about that type. Scalar types provide no information beyond a name and description, while Enum types provide their values. Object and Interface types provide the fields they describe. Abstract types, Union and Interface, provide the Object types possible at runtime. List and NonNull types compose other types.
""""""
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

""""""
An enum describing what kind of type a given __Type is.
""""""
enum __TypeKind {
  """"""
  Indicates this type is a scalar.
  """"""
  SCALAR
  """"""
  Indicates this type is an object. `fields` and `possibleTypes` are valid fields.
  """"""
  OBJECT
  """"""
  Indicates this type is an interface. `fields` and `possibleTypes` are valid fields.
  """"""
  INTERFACE
  """"""
  Indicates this type is a union. `possibleTypes` is a valid field.
  """"""
  UNION
  """"""
  Indicates this type is an enum. `enumValues` is a valid field.
  """"""
  ENUM
  """"""
  Indicates this type is an input object. `inputFields` is a valid field.
  """"""
  INPUT_OBJECT
  """"""
  Indicates this type is a list. `ofType` is a valid field.
  """"""
  LIST
  """"""
  Indicates this type is a non-null. `ofType` is a valid field.
  """"""
  NON_NULL
}
";

        AssertEqual(result, "", expected, excludeScalars: true);
    }

    [Fact]
    public void prints_introspection_schema_with_experimental_features_enabled()
    {
        var schema = new Schema
        {
            Query = new ObjectGraphType
            {
                Name = "Root"
            }
        }
        .EnableExperimentalIntrospectionFeatures();
        schema.Query.Fields.Add(new FieldType { Name = "unused", ResolvedType = new StringGraphType() });
        var printer = new SchemaPrinter(schema, new SchemaPrinterOptions { IncludeDescriptions = true, PrintDescriptionsAsComments = true });
        var result = Environment.NewLine + printer.PrintIntrospectionSchema();

        const string expected = @"
schema {
  query: Root
}

# Marks an element of a GraphQL schema as no longer supported.
directive @deprecated(
  reason: String = ""No longer supported""
) on FIELD_DEFINITION | ENUM_VALUE

# Directs the executor to include this field or fragment only when the 'if' argument is true.
directive @include(
  if: Boolean!
) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT

# Directs the executor to skip this field or fragment when the 'if' argument is true.
directive @skip(
  if: Boolean!
) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT

# Directive applied to a schema element
type __AppliedDirective {
  # Directive name
  name: String!
  # Values of explicitly specified directive arguments
  args: [__DirectiveArgument!]!
}

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
  isRepeatable: Boolean!
  onOperation: Boolean!
  onFragment: Boolean!
  onField: Boolean!
  # Directives applied to the directive
  appliedDirectives: [__AppliedDirective!]!
}

# Value of an argument provided to directive
type __DirectiveArgument {
  # Argument name
  name: String!
  # A GraphQL-formatted string representing the value for argument.
  value: String!
}

# A Directive can be adjacent to many parts of the GraphQL language, a
# __DirectiveLocation describes one such possible adjacencies.
enum __DirectiveLocation {
  # Location adjacent to a query operation.
  QUERY
  # Location adjacent to a mutation operation.
  MUTATION
  # Location adjacent to a subscription operation.
  SUBSCRIPTION
  # Location adjacent to a field.
  FIELD
  # Location adjacent to a fragment definition.
  FRAGMENT_DEFINITION
  # Location adjacent to a fragment spread.
  FRAGMENT_SPREAD
  # Location adjacent to an inline fragment.
  INLINE_FRAGMENT
  # Location adjacent to a variable definition.
  VARIABLE_DEFINITION
  # Location adjacent to a schema definition.
  SCHEMA
  # Location adjacent to a scalar definition.
  SCALAR
  # Location adjacent to an object type definition.
  OBJECT
  # Location adjacent to a field definition.
  FIELD_DEFINITION
  # Location adjacent to an argument definition.
  ARGUMENT_DEFINITION
  # Location adjacent to an interface definition.
  INTERFACE
  # Location adjacent to a union definition.
  UNION
  # Location adjacent to an enum definition
  ENUM
  # Location adjacent to an enum value definition
  ENUM_VALUE
  # Location adjacent to an input object type definition.
  INPUT_OBJECT
  # Location adjacent to an input object field definition.
  INPUT_FIELD_DEFINITION
}

# One possible value for a given Enum. Enum values are unique values, not a
# placeholder for a string or numeric value. However an Enum value is returned in
# a JSON response as a string.
type __EnumValue {
  name: String!
  description: String
  isDeprecated: Boolean!
  deprecationReason: String
  # Directives applied to the enum value
  appliedDirectives: [__AppliedDirective!]!
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
  # Directives applied to the field
  appliedDirectives: [__AppliedDirective!]!
}

# Arguments provided to Fields or Directives and the input fields of an
# InputObject are represented as Input Values which describe their type and
# optionally a default value.
type __InputValue {
  name: String!
  description: String
  type: __Type!
  # A GraphQL-formatted string representing the default value for this input value.
  defaultValue: String
  # Directives applied to the input value
  appliedDirectives: [__AppliedDirective!]!
}

# A GraphQL Schema defines the capabilities of a GraphQL server. It exposes all
# available types and directives on the server, as well as the entry points for
# query, mutation, and subscription operations.
type __Schema {
  description: String
  # A list of all types supported by this server.
  types: [__Type!]!
  # The type that query operations will be rooted at.
  queryType: __Type!
  # If this server supports mutation, the type that mutation operations will be rooted at.
  mutationType: __Type
  # If this server supports subscription, the type that subscription operations will be rooted at.
  subscriptionType: __Type
  # A list of all directives supported by this server.
  directives: [__Directive!]!
  # Directives applied to the schema
  appliedDirectives: [__AppliedDirective!]!
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
  # Directives applied to the type
  appliedDirectives: [__AppliedDirective!]!
}

# An enum describing what kind of type a given __Type is.
enum __TypeKind {
  # Indicates this type is a scalar.
  SCALAR
  # Indicates this type is an object. `fields` and `possibleTypes` are valid fields.
  OBJECT
  # Indicates this type is an interface. `fields` and `possibleTypes` are valid fields.
  INTERFACE
  # Indicates this type is a union. `possibleTypes` is a valid field.
  UNION
  # Indicates this type is an enum. `enumValues` is a valid field.
  ENUM
  # Indicates this type is an input object. `inputFields` is a valid field.
  INPUT_OBJECT
  # Indicates this type is a list. `ofType` is a valid field.
  LIST
  # Indicates this type is a non-null. `ofType` is a valid field.
  NON_NULL
}
";

        AssertEqual(result, "", expected, excludeScalars: true);
    }

    [Fact]
    public void prints_descriptions_correctly()
    {
        var printer = new SchemaPrinter(new Schema());
        printer.PrintDescription("This is a test").ShouldBeCrossPlat("\"\"\"\nThis is a test\n\"\"\"\n");
        printer.PrintDescription("Th\\is \"is\" a \"\"\"test\n\tline2\n line3\u0003").ShouldBeCrossPlat("\"\"\"\nTh\\is \"is\" a \\\"\"\"test\n\tline2\n line3\n\"\"\"\n");
    }

    [Fact]
    public void sorts_schema_correctly()
    {
        var schema = Schema.For(@"
# test sorting type names
type Zebra {
  # test sorting field names on object types
  field3: String
  field2: Int
}

type Query {
  field1(arg1: Rutabaga, arg2: Beta): Zebra
  # test sorting arguments
  field2(arg2: Rutabaga, arg1: Beta): Tango
  # test sorting fields of default values
  field3(arg3: Rutabaga = { field3: ""hello"", field2: 2 }): String
}

type Tango {
  field1: Int
  field2: Int
}

input Rutabaga {
  # test sorting field names on input types
  field3: String
  field2: Int
}

# test sorting directives
directive @test2(
  arg1: Boolean!
  arg2: Boolean!
  # test sorting directive locations -- does not work yet
) on INLINE_FRAGMENT | FIELD | FRAGMENT_SPREAD

directive @test1(
  # test sorting fields within directives -- does not work yet
  arg2: Boolean!
  arg1: Boolean!
) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT

enum Beta {
  # test sorting of enum value names
  VALUE_3
  VALUE_2
}
");
        var printer = new SchemaPrinter(schema, new SchemaPrinterOptions { Comparer = new GraphQL.Introspection.AlphabeticalSchemaComparer() });
        var actual = printer.Print();
        var expected = @"directive @test1(
  arg2: Boolean!
  arg1: Boolean!
) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT

directive @test2(
  arg1: Boolean!
  arg2: Boolean!
) on INLINE_FRAGMENT | FIELD | FRAGMENT_SPREAD

enum Beta {
  VALUE_2
  VALUE_3
}

type Query {
  field1(arg1: Rutabaga, arg2: Beta): Zebra
  field2(arg1: Beta, arg2: Rutabaga): Tango
  field3(arg3: Rutabaga = { field2: 2, field3: ""hello"" }): String
}

input Rutabaga {
  field2: Int
  field3: String
}

type Tango {
  field1: Int
  field2: Int
}

type Zebra {
  field2: Int
  field3: String
}
";
        actual.ShouldBe(expected, StringCompareShould.IgnoreLineEndings);
    }

    public class FooType : ObjectGraphType
    {
        public FooType()
        {
            Name = "Foo";
            Description = "This is a Foo object type";
            Field<StringGraphType>(
                name: "str",
                description: "This is of type String");
            Field<IntGraphType>(
                name: "int",
                description: "This is of type Integer",
                deprecationReason: "This field is now deprecated");
        }
    }

    public class FooInterfaceType : InterfaceGraphType
    {
        public FooInterfaceType()
        {
            Name = "IFoo";
            Description = "This is a Foo interface type";
            ResolveType = obj => null;
            Field<StringGraphType>(
                name: "str",
                description: "This is of type String");
        }
    }

    public class BaazInterfaceType : InterfaceGraphType
    {
        public BaazInterfaceType()
        {
            Name = "Baaz";
            ResolveType = obj => null;
            Field<IntGraphType>(
                name: "int",
                description: "This is of type Integer");
        }
    }

    public class BarType : ObjectGraphType
    {
        public BarType()
        {
            Name = "Bar";
            Field<StringGraphType>(
                name: "str",
                description: "This is of type String");
            Interface<FooInterfaceType>();
        }
    }

    public class BarMultipleType : ObjectGraphType
    {
        public BarMultipleType()
        {
            Name = "Bar";
            Field<StringGraphType>(
                name: "str",
                description: "This is of type String");
            Field<IntGraphType>(
              name: "int",
              description: "This is of type Integer");
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

    public class SomeInputType : InputObjectGraphType<SomeInput>
    {
        public SomeInputType()
        {
            Name = "SomeInput";
            Field(x => x.Age);
            Field(x => x.Name);
            Field(x => x.IsDeveloper);
        }
    }

    public class SomeInput
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public bool IsDeveloper { get; set; }
    }

    public class SomeInputType2 : InputObjectGraphType<SomeInput2>
    {
        public SomeInputType2()
        {
            Name = "SomeInput2";
            Field(x => x.Names, true);
        }
    }

    public class SomeInput2
    {
        public IList<string> Names { get; set; }
    }

    public class OddType : ScalarGraphType
    {
        public OddType()
        {
            Name = "Odd";
        }

        public override object ParseValue(object value) => null;

        public override object ParseLiteral(GraphQLValue value) => null;
    }

    public class RgbEnum : EnumerationGraphType
    {
        public RgbEnum()
        {
            Name = "RGB";
            Add("RED", 0, "Red!", "Use green!");
            Add("GREEN", 1, "Green!");
            Add("BLUE", 2, "Blue!");
        }
    }
}
