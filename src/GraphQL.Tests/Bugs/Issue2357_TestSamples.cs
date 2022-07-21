using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/pulls/2357
public class Issue2357_TestSamples : QueryTestBase<Issue2357_TestSamples.MySchema>
{
    [Fact]
    public void output_null()
    {
        AssertQuerySuccess("{ testDbOutputNull }", "{\"testDbOutputNull\": null}");
    }

    [Fact]
    public void output_value()
    {
        AssertQuerySuccess("{ testDbOutputValue }", "{\"testDbOutputValue\": \"123\"}");
    }

    [Fact]
    public void input_literal_null()
    {
        AssertQuerySuccess("{ testDbInput(arg:null) }", "{\"testDbInput\": \"0\"}");
    }

    [Fact]
    public void input_literal_value()
    {
        AssertQuerySuccess("{ testDbInput(arg:\"123\") }", "{\"testDbInput\": \"123\"}");
    }

    [Fact]
    public void input_value_null()
    {
        AssertQuerySuccess("query ($arg: DbId) { testDbInput(arg:$arg) }", "{\"testDbInput\": \"0\"}", "{\"arg\":null}".ToInputs());
    }

    [Fact]
    public void input_value_value()
    {
        AssertQuerySuccess("query ($arg: DbId) { testDbInput(arg:$arg) }", "{\"testDbInput\": \"123\"}", "{\"arg\":\"123\"}".ToInputs());
    }

    public class MySchema : Schema
    {
        public MySchema()
        {
            Query = new MyQuery();
        }
    }

    public class MyQuery : ObjectGraphType
    {
        public MyQuery()
        {
            Field<DbIdGraphType>("testDbOutputNull").Resolve(_ => 0);
            Field<DbIdGraphType>("testDbOutputValue").Resolve(_ => 123);
            Field<StringGraphType>("testDbInput")
                .Argument<DbIdGraphType>("arg")
                .Resolve(context => context.GetArgument<int>("arg").ToString());
        }
    }

    public class DbIdGraphType : ScalarGraphType
    {
        public DbIdGraphType()
        {
            Name = "DbId";
        }

        public override object ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLStringValue s => int.TryParse((string)s.Value, out int i) && i > 0 ? i : throw new FormatException($"'{s.Value}' is not a valid identifier."), // string conversion for NET48
            GraphQLNullValue _ => 0,
            _ => ThrowLiteralConversionError(value)
        };

        public override object ParseValue(object value) => value switch
        {
            string s => int.TryParse(s, out int i) && i > 0 ? i : throw new FormatException($"'{s}' is not a valid identifier."),
            null => 0,
            _ => ThrowValueConversionError(value)
        };

        public override object Serialize(object value) => value switch
        {
            int i => i > 0 ? i.ToString() : i == 0 ? null : ThrowSerializationError(value),
            _ => ThrowSerializationError(value)
        };
    }
}
