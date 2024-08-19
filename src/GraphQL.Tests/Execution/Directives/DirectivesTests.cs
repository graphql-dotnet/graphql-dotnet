using GraphQL.Types;

namespace GraphQL.Tests.Execution.Directives;

public class DirectiveSchema : Schema
{
    public DirectiveSchema()
    {
        Query = new DirectiveTestType();
        Directives.Register(new Directive("test1", GraphQLParser.AST.DirectiveLocation.Field));
        Directives.Register(new Directive("test2", GraphQLParser.AST.DirectiveLocation.Field)
        {
            Arguments = new QueryArguments([new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "value" }])
        });
    }
}

public class DirectiveTestType : ObjectGraphType
{
    public DirectiveTestType()
    {
        Name = "TestType";

        Field<StringGraphType>("a");
        Field<StringGraphType>("b");
        Field<BooleanGraphType>("c")
            .Resolve(context => context.GetDirective("test1") != null);
        Field<StringGraphType>("d")
            .Resolve(context => context.GetDirective("test2")?.GetArgument<string>("value"));
    }
}

public class DirectiveData
{
    public DirectiveData()
    {
        A = "a";
        B = "b";
    }

    public string A { get; set; }

    public string B { get; set; }
}

public class DirectiveScalarTests : QueryTestBase<DirectiveSchema>
{
    private readonly DirectiveData _data = new();

    [Fact]
    public void works_without_directives()
    {
        AssertQuerySuccess("{a, b}", """{"a": "a", "b": "b"}""", null, _data);
    }

    [Fact]
    public void works_on_scalars()
    {
        AssertQuerySuccess("{a, b @include(if: true) }", """{"a": "a", "b": "b"}""", null, _data);
    }

    [Fact]
    public void if_false_omits_on_scalar()
    {
        AssertQuerySuccess("{a, b @include(if: false) }", """{"a": "a"}""", null, _data);
    }

    [Fact]
    public void skip_false_includes_scalar()
    {
        AssertQuerySuccess("{a, b @skip(if: false) }", """{"a": "a", "b": "b"}""", null, _data);
    }

    [Fact]
    public void skip_true_omits_scalar()
    {
        AssertQuerySuccess("{a, b @skip(if: true) }", """{"a": "a"}""", null, _data);
    }

    [Fact]
    public void skip_true_include_true_omits_scalar()
    {
        AssertQuerySuccess("{a, b @skip(if: true) @include(if: true) }", """{"a": "a"}""", null, _data);
    }

    [Fact]
    public void skip_false_include_false_omits_scalar()
    {
        AssertQuerySuccess("{a, b @skip(if: false) @include(if: false) }", """{"a": "a"}""", null, _data);
    }

    [Fact]
    public void skip_true_include_false_omits_scalar()
    {
        AssertQuerySuccess("{a, b @skip(if: true) @include(if: false) }", """{"a": "a"}""", null, _data);
    }

    [Fact]
    public void skip_false_include_true_includes_scalar()
    {
        AssertQuerySuccess("{a, b @skip(if: false) @include(if: true) }", """{"a": "a", "b": "b"}""", null, _data);
    }
}

public class DirectiveFragmentTests : QueryTestBase<DirectiveSchema>
{
    private readonly DirectiveData _data = new();

    [Fact]
    public void if_false_omits_fragment_spread()
    {
        AssertQuerySuccess("""
            query Q {
              a
              ...Frag @include(if: false)
            }
            fragment Frag on TestType {
              b
            }
            """,
            """
            {
              "a": "a"
            }
            """,
        null, _data);
    }

    [Fact]
    public void if_true_includes_fragment_spread()
    {
        AssertQuerySuccess("""
            query Q {
              a
              ...Frag @include(if: true)
            }
            fragment Frag on TestType {
              b
            }
            """,
            """
            {
              "a": "a",
              "b": "b"
            }
            """,
            null, _data);
    }

    [Fact]
    public void skip_false_includes_fragment_spread()
    {
        AssertQuerySuccess("""
            query Q {
              a
              ...Frag @skip(if: false)
            }
            fragment Frag on TestType {
              b
            }
            """,
            """
            {
              "a": "a",
              "b":"b"
            }
            """,
            null, _data);
    }

    [Fact]
    public void skip_true_omits_fragment_spread()
    {
        AssertQuerySuccess("""
            query Q {
              a
              ...Frag @skip(if: true)
            }
            fragment Frag on TestType {
              b
            }
            """,
            """
            {
              "a": "a"
            }
            """,
            null, _data);
    }

    [Fact]
    public void if_false_omits_inline_fragment()
    {
        AssertQuerySuccess("""
            query Q {
              a
              ... on TestType @include(if: false) {
                b
              }
            }
            """,
            """
            {
              "a": "a"
            }
            """,
            null, _data);
    }

    [Fact]
    public void if_true_includes_inline_fragment()
    {
        AssertQuerySuccess("""
            query Q {
              a
              ... on TestType @include(if: true) {
                b
              }
            }
            """,
            """
            {
              "a": "a",
              "b": "b"
            }
            """,
            null, _data);
    }

    [Fact]
    public void skip_true_omits_inline_fragment()
    {
        AssertQuerySuccess("""
            query Q {
              a
              ... on TestType @skip(if: true) {
                b
              }
            }
            """,
            """
            {
              "a": "a"
            }
            """,
            null, _data);
    }

    [Fact]
    public void skip_false_includes_inline_fragment()
    {
        AssertQuerySuccess("""
            query Q {
              a
              ... on TestType @skip(if: false) {
                b
              }
            }
            """,
            """
            {
              "a": "a",
              "b": "b"
            }
            """,
            null, _data);
    }

    [Fact]
    public void if_false_omits_fragment()
    {
        AssertQuerySuccess("""
            query Q {
              a
              ...Frag @include(if: false)
            }
            fragment Frag on TestType {
              b
            }
            """,
            """
            {
              "a": "a"
            }
            """,
            null, _data);
    }

    [Fact]
    public void if_true_includes_fragment()
    {
        AssertQuerySuccess("""
            query Q {
              a
              ...Frag @include(if: true)
            }
            fragment Frag on TestType {
              b
            }
            """,
            """
            {
              "a": "a",
              "b": "b"
            }
            """,
            null, _data);
    }

    [Fact]
    public void skip_false_includes_fragment()
    {
        AssertQuerySuccess("""
            query Q {
              a
              ...Frag @skip(if: false)
            }
            fragment Frag on TestType {
              b
            }
            """,
            """
            {
              "a": "a",
              "b": "b"
            }
            """,
            null, _data);
    }

    [Fact]
    public void skip_true_omits_fragment()
    {
        AssertQuerySuccess("""
            query Q {
              a
              ...Frag @skip(if: true)
            }
            fragment Frag on TestType {
              b
            }
            """,
            """
            {
              "a": "a"
            }
            """,
            null, _data);
    }
}

public class DirectiveParsingTests : QueryTestBase<DirectiveSchema>
{
    [Fact]
    public void finds_directive()
    {
        AssertQuerySuccess("""
            query {
              c @test1
            }
            """,
            """
            {
              "c": true
            }
            """,
            null, null);
    }

    [Fact]
    public void finds_directive_with_argument()
    {
        AssertQuerySuccess("""
            query {
              d @test2(value: "hello")
            }
            """,
            """
            {
              "d": "hello"
            }
            """,
            null, null);
    }
}
