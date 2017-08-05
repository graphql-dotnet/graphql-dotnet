using GraphQL.Types;

namespace GraphQL.StarWars.Types
{
    public class DroidType : AutoRegisteringObjectGraphType<Droid>
    {
        public DroidType(StarWarsData data)
        {
            Name = "Droid";
            Description = "A mechanical creature in the Star Wars universe.";
            
            Field<ListGraphType<CharacterInterface>>(
                "friends",
                resolve: context => data.GetFriends(context.Source)
            );
            Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");

            Interface<CharacterInterface>();
        }
    }
}
