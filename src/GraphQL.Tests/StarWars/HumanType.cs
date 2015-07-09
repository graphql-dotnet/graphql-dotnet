namespace GraphQL.Tests
{
    public class HumanType : ObjectGraphType
    {
        public HumanType()
        {
            var data = new StarWarsData();

            Name = "Human";

            Field("id", "The id of the human.", NonNullGraphType.String);
            Field("name", "The name of the human.", NonNullGraphType.String);
            Field<ListGraphType<CharacterInterface>>(
                "friends",
                resolve: context => data.GetFriends(context.Source as StarWarsCharacter)
            );

            Interface<CharacterInterface>();
        }
    }
}
