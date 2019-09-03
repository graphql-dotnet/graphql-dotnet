using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/issues/1285
    public class Issue1285 : QueryTestBase<Issue1285Schema>
    {
        [Fact]
        public void Issue1285_Should_Work()
        {
            var query = @"
query {
  getsome(input: { name: ""value"" })
}
";
            var expected = @"{
  ""getsome"": null
}";
            AssertQuerySuccess(query, expected, null);
        }
    }

    public class Issue1285Schema : Schema
    {
        public Issue1285Schema()
        {
            Query = new Issue1285Query();
        }
    }

    public class Issue1285Query : ObjectGraphType
    {
        public Issue1285Query()
        {
            Field<ListGraphType<IntGraphType>>(
                "getsome",
                arguments: new QueryArguments(
                    new QueryArgument<ArrayInputType> { Name = "input" }
                ),
                resolve: ctx =>
                {
                    var arg = ctx.GetArgument<ArrayInput>("input");
                    return arg.Ints;
                });
        }
    }

    public class ArrayInput
    {
        public int[] Ints { get; set; }

        public string Name { get; set; }
    }

    public class ArrayInputType : InputObjectGraphType<ArrayInput>
    {
        public ArrayInputType()
        {
            Field("ints", o => o.Ints, nullable: true);
            Field("name", o => o.Name);
        }
    }
}
