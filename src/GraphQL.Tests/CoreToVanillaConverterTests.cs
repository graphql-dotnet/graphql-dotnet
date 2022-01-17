using System;
using System.Threading.Tasks;
using GraphQL.Language;
using GraphQL.Types;
using GraphQLParser.AST;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/pulls/1781
    public class CoreToVanillaConverterTests : QueryTestBase<PR1781Schema>
    {
        [Fact]
        public async Task DocumentExecuter_really_big_double_valid()
        {
            var de = new DocumentExecuter();
            var valid = await de.ExecuteAsync(new ExecutionOptions
            {
                // create a floating-point value that is larger than double.MaxValue
                // in the expression "{double.MaxValue:0}0.0" below, the 0.0 effectively
                // multiplies double.MaxValue by 10 and the .0 forces the parser to
                // assume it is a floating point value rather than a large integer
                Query = $"{{ test(arg:{double.MaxValue:0}0.0) }}",
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
                Query = $"{{ test(arg:{double.MinValue:0}0.0) }}",
                Schema = Schema,
            });
            valid.ShouldNotBeNull();
            valid.Data.ShouldNotBeNull();
            valid.Errors.ShouldBeNull();
        }

        [Theory(Skip = "Should be moved into parser")]
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
            GraphQLValue BuildNode()
            {
                return kind switch
                {
                    ASTNodeKind.IntValue => new GraphQLIntValue(),
                    ASTNodeKind.FloatValue => new GraphQLFloatValue(),
                    ASTNodeKind.BooleanValue => new GraphQLBooleanValue(),
                    _ => throw new NotSupportedException(),
                };
            }

            //note: thousand separators and/or culture-specific characters are invalid graphql literals, and will not be returned by graphql-parser
            //uppercase TRUE and FALSE are also invalid graphql input data, and will not be returned by graphql-parser
            //whitespace will not be returned by graphql-parser
            dynamic node = BuildNode();
            node.Value = valueString;
            var _ = node.ClrValue;
        }
    }

    public class PR1781Schema : Schema
    {
        public PR1781Schema()
        {
            Query = new PR1781Query();
        }
    }

    public class PR1781Query : ObjectGraphType
    {
        public PR1781Query()
        {
            Field<StringGraphType>("Test",
                resolve: context => "ok",
                arguments: new QueryArguments(
                    new QueryArgument(typeof(FloatGraphType)) { Name = "arg" }
                ));
        }
    }
}
