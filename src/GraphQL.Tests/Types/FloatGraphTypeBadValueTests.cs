using System;
using System.Threading.Tasks;
using GraphQL.Types;
using GraphQLParser.AST;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/pulls/1781
    public class FloatGraphTypeBadValueTests : QueryTestBase<PR1781Schema>
    {
        [Fact]
        public void BadFloatValues()
        {
            var type = new FloatGraphType();

            var literal1 = $"{double.MaxValue:0}0.0";
            var literal2 = $"{double.MinValue:0}0.0";

            var value1 = new GraphQLFloatValue(literal1);
            var value2 = new GraphQLFloatValue(literal2);

            type.CanParseLiteral(value1).ShouldBeFalse();
            type.CanParseLiteral(value2).ShouldBeFalse();

            Should.Throw<InvalidOperationException>(() => type.ParseLiteral(value1)).Message.ShouldStartWith("Unable to convert '1797693134862320000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000.0' literal from AST representation to the scalar type 'Float'");
            Should.Throw<InvalidOperationException>(() => type.ParseLiteral(value2)).Message.ShouldStartWith("Unable to convert '-1797693134862320000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000.0' literal from AST representation to the scalar type 'Float'");
        }

        [Fact]
        public async Task DocumentExecuter_really_big_double_Invalid()
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
            valid.Data.ShouldBeNull();
            valid.Errors.ShouldNotBeNull();
            valid.Errors.Count.ShouldBe(1);
            valid.Errors[0].Message.ShouldBe($"Argument 'arg' has invalid value. Expected type 'Float', found {double.MaxValue:0}0.0.");
        }

        [Fact]
        public async Task DocumentExecuter_really_small_double_Invalid()
        {
            var de = new DocumentExecuter();
            var valid = await de.ExecuteAsync(new ExecutionOptions
            {
                Query = $"{{ test(arg:{double.MinValue:0}0.0) }}",
                Schema = Schema,
            });
            valid.ShouldNotBeNull();
            valid.Data.ShouldBeNull();
            valid.Errors.ShouldNotBeNull();
            valid.Errors.Count.ShouldBe(1);
            valid.Errors[0].Message.ShouldBe($"Argument 'arg' has invalid value. Expected type 'Float', found {double.MinValue:0}0.0.");
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
