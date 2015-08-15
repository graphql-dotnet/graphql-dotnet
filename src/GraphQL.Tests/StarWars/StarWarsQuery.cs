using GraphQL.Types;

namespace GraphQL.Tests
{
    public class StarWarsQuery : ObjectGraphType
    {
        public StarWarsQuery()
        {
            var data = new StarWarsData();

            Name = "Query";

            Field<CharacterInterface>("hero", resolve: context => data.GetDroidById("3"));
            Field<HumanType>(
                "human",
                arguments: new QueryArguments(
                    new []
                    {
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
                    }),
                resolve: context => data.GetHumanById((string)context.Arguments["id"])
            );
            Field<DroidType>(
                "droid",
                arguments: new QueryArguments(
                    new []
                    {
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
                    }),
                resolve: context => data.GetDroidById((string)context.Arguments["id"])
            );
        }
    }
}
