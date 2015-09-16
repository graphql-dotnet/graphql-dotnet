using GraphQL.Types;

namespace GraphQL.Tests
{
    public class StarWarsQuery : ObjectGraphType
    {
        public StarWarsQuery()
        {
            var data = new StarWarsData();

            Name = "Query";

            Field<CharacterInterface>("hero", resolve: context => data.GetDroidByIdAsync("3"));
            Field<HumanType>(
                "human",
                arguments: new QueryArguments(
                    new []
                    {
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the human" }
                    }),
                resolve: context => data.GetHumanByIdAsync((string)context.Arguments["id"])
            );
            Field<DroidType>(
                "droid",
                arguments: new QueryArguments(
                    new []
                    {
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the droid" }
                    }),
                resolve: context => data.GetDroidByIdAsync((string)context.Arguments["id"])
            );
        }
    }
}
