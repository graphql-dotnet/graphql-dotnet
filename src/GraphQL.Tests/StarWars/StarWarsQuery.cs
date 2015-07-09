namespace GraphQL.Tests
{
    public class StarWarsQuery : ObjectGraphType
    {
        public StarWarsQuery()
        {
            var data = new StarWarsData();

            Field<CharacterInterface>("hero", resolve: context => data.GetDroidById("3"));
            Field<HumanType>(
                "human",
                arguments: new QueryArguments(
                    new []
                    {
                        new QueryArgument { Name = "id", Type = NonNullGraphType.String}
                    }),
                resolve: context => data.GetHumanById((string)context.Arguments["id"])
            );
            Field<DroidType>(
                "droid",
                arguments: new QueryArguments(
                    new []
                    {
                        new QueryArgument { Name = "id", Type = NonNullGraphType.String}
                    }),
                resolve: context => data.GetDroidById((string)context.Arguments["id"])
            );
        }
    }
}