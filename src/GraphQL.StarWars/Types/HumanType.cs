using GraphQL.Types;

namespace GraphQL.StarWars.Types
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
            Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movies they appear in.");
            Field<StringGraphType>("homePlanet", "The home planet of the human.");

            Interface<CharacterInterface>();

            IsTypeOf = value => value is Human;
        }
    }
}
