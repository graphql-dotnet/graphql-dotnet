using GraphQL.Types;

namespace GraphQL.Tests
{
    public class HumanType : ObjectGraphType
    {
        public HumanType(StarWarsData data)
        {
            Name = "Human";

            Field<NonNullGraphType<StringGraphType>>("id", "The id of the human.");
            Field<StringGraphType>("name", "The name of the human.");
            Field<ListGraphType<CharacterInterface>>(
                "friends",
                resolve: context => data.GetFriends(context.Source as StarWarsCharacter)
            );
            Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");
            Field<StringGraphType>("homePlanet", "The home planet of the human.");

            Interface<CharacterInterface>();
        }
    }
}
