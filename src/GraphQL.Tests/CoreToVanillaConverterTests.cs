using System.Threading.Tasks;
using GraphQL.Types;
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
