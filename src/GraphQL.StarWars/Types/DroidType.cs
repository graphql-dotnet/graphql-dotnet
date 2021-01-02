using GraphQL.StarWars.Extensions;
using GraphQL.Types;

namespace GraphQL.StarWars.Types
{
    public class DroidType : ObjectGraphType<Droid>
    {
        public DroidType(StarWarsData data)
        {
            Name = "Droid";
            Description = "A mechanical creature in the Star Wars universe.";

            Field(d => d.Id).Description("The id of the droid.");
            Field(d => d.Name, nullable: true).Description("The name of the droid.");

            Field<ListGraphType<CharacterInterface>>(
                "friends",
                resolve: context =>
                {
                    // simulate loading
                    // await Task.Delay(1000);
                    return data.GetFriends(context.Source);
                }
            );

            Connection<CharacterInterface>()
                .Name("friendsConnection")
                .Description("A list of a character's friends.")
                .Bidirectional()
                .Resolve(context => context.GetPagedResults<Droid, StarWarsCharacter>(data, context.Source.Friends));

            Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");
            Field(d => d.PrimaryFunction, nullable: true).Description("The primary function of the droid.");

            Interface<CharacterInterface>();
        }
    }
}
