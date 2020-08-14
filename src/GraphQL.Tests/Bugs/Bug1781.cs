using System.Numerics;
using System.Threading.Tasks;
using GraphQL.Language;
using GraphQL.Types;
using GraphQLParser.AST;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/pulls/1781
    public class Bug1781 : QueryTestBase<Bug1781Schema>
    {
        [Fact]
        public async Task DocumentExecuter_really_big_double_valid()
        {
            var de = new DocumentExecuter();
            var valid = await de.ExecuteAsync(new ExecutionOptions
            {
                Query = $"{{ test(arg:{double.MaxValue.ToString("0")}0.0) }}",
                Schema = Schema,
            });
            valid.ShouldNotBeNull();
            valid.Data.ShouldNotBeNull();
            valid.Errors.ShouldBeNull();
        }

        [Fact]
        public async Task DocumentExecuter_really_small_double_valid()
        {
            var de = new DocumentExecuter();
            var valid = await de.ExecuteAsync(new ExecutionOptions
            {
                Query = $"{{ test(arg:{double.MinValue.ToString("0")}0.0) }}",
                Schema = Schema,
            });
            valid.ShouldNotBeNull();
            valid.Data.ShouldNotBeNull();
            valid.Errors.ShouldBeNull();
        }

        [Theory]
        [InlineData(ASTNodeKind.IntValue, "1")]
        [InlineData(ASTNodeKind.IntValue, "-1")]
        [InlineData(ASTNodeKind.IntValue, "9223372036854775807")]
        [InlineData(ASTNodeKind.IntValue, "-9223372036854775808")]
        [InlineData(ASTNodeKind.IntValue, "79228162514264337593543950335")]
        [InlineData(ASTNodeKind.IntValue, "-79228162514264337593543950335")]
        [InlineData(ASTNodeKind.IntValue, "100000000000000000000000000000000")]
        [InlineData(ASTNodeKind.IntValue, "-100000000000000000000000000000000")]
        [InlineData(ASTNodeKind.FloatValue, "1.7976931348623157E+308")]
        [InlineData(ASTNodeKind.FloatValue, "-1.7976931348623157E+308")]
        [InlineData(ASTNodeKind.FloatValue, "1e+5")]
        [InlineData(ASTNodeKind.FloatValue, "1e-5")]
        [InlineData(ASTNodeKind.FloatValue, "1e5")]
        [InlineData(ASTNodeKind.FloatValue, "1.0")]
        [InlineData(ASTNodeKind.FloatValue, "1.")]
        [InlineData(ASTNodeKind.FloatValue, "1.7976931348623157E+900")]
        [InlineData(ASTNodeKind.FloatValue, "-1.7976931348623157E+900")]
        [InlineData(ASTNodeKind.BooleanValue, "true")]
        [InlineData(ASTNodeKind.BooleanValue, "false")]
        public void Values_Parse_Successfully(ASTNodeKind kind, string valueString)
        {
            //note: thousand separators and/or culture-specific characters are invalid graphql literals, and will not be returned by graphql-parser
            //uppercase TRUE and FALSE are also invalid graphql input data, and will not be returned by graphql-parser
            //whitespace will not be returned by graphql-parser
            var converter = new CoreToVanillaConverter("");
            var value = new GraphQLScalarValue(kind)
            {
                Value = valueString
            };
            var ret = converter.Value(value);
        }
    }

    public class Bug1781Schema : Schema
    {
        public Bug1781Schema()
        {
            Query = new Bug1781Query();
        }
    }

    public class Bug1781Query : ObjectGraphType
    {
        public Bug1781Query()
        {
            Field<StringGraphType>("Test",
                resolve: context => "ok",
                arguments: new QueryArguments(
                    new QueryArgument(typeof(FloatGraphType)) { Name = "arg" }
                ));
        }
    }
}
