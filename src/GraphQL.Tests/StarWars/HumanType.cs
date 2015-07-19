using GraphQL.Types;

namespace GraphQL.Tests
{
    public class HumanType : ObjectGraphType
    {
        public HumanType()
        {
            var data = new StarWarsData();

            Name = "Human";

            Field("id", "The id of the human.", NonNullGraphType.String);
            Field("name", "The name of the human.", ScalarGraphType.String);
            Field<ListGraphType<CharacterInterface>>(
                "friends",
                resolve: context => data.GetFriends(context.Source as StarWarsCharacter)
            );
            Field("appearsIn", "Which movie they appear in.", new ListGraphType<EpisodeEnum>());
            Field("homePlanet", "The home planet of the human.", ScalarGraphType.String);

            Interface<CharacterInterface>();
        }
    }
}
