using GraphQL.Types;

namespace GraphQL.StarWars.Types
{
    public class HumanType : AutoRegisteringObjectGraphType<Human>
    {
        public HumanType(StarWarsData data)
        {
            Name = "Human";

            Field<ListGraphType<CharacterInterface>>(
                "friends",
                resolve: context => data.GetFriends(context.Source)
            );
            Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");

            Interface<CharacterInterface>();
        }
    }
}
