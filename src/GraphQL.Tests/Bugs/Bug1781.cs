using System.Threading.Tasks;
using GraphQL.Types;
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
