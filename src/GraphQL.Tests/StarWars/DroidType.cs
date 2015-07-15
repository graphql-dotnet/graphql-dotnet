using GraphQL.Types;

namespace GraphQL.Tests
{
    public class DroidType : ObjectGraphType
    {
        public DroidType()
        {
            var data = new StarWarsData();

            Name = "Droid";

            Field("id", "The id of the droid.", NonNullGraphType.String);
            Field("name", "The name of the droid.", NonNullGraphType.String);
            Field<ListGraphType<CharacterInterface>>(
                "friends",
                resolve: context => data.GetFriends(context.Source as StarWarsCharacter)
            );

            Interface<CharacterInterface>();
        }
    }
}
