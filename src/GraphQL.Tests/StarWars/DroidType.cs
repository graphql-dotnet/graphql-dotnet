using GraphQL.Types;

namespace GraphQL.Tests
{
    public class DroidType : ObjectGraphType
    {
        public DroidType()
        {
            var data = new StarWarsData();

            Name = "Droid";
            Description = "A mechanical creature in the Star Wars universe.";

            Field("id", "The id of the droid.", NonNullGraphType.String);
            Field("name", "The name of the droid.", ScalarGraphType.String);
            Field<ListGraphType<CharacterInterface>>(
                "friends",
                resolve: context => data.GetFriends(context.Source as StarWarsCharacter)
            );
            Field("appearsIn", "Which movie they appear in.", new ListGraphType<EpisodeEnum>());
            Field("primaryFunction", "The primary function of the droid.", ScalarGraphType.String);

            Interface<CharacterInterface>();
        }
    }
}
