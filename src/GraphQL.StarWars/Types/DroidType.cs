using GraphQL.StarWars.Extensions;
using GraphQL.Types;

namespace GraphQL.StarWars.Types;

public class DroidType : ObjectGraphType<Droid>
{
    public DroidType(StarWarsData data)
    {
        Name = "Droid";
        Description = "A mechanical creature in the Star Wars universe.";

        Field<NonNullGraphType<StringGraphType>>("id").Description("The id of the droid.").Resolve(context => context.Source.Id);
        Field<StringGraphType>("name").Description("The name of the droid.").Resolve(context => context.Source.Name);

        Field<ListGraphType<CharacterInterface>>("friends").Resolve(context => data.GetFriends(context.Source));

        Connection<CharacterInterface>()
            .Name("friendsConnection")
            .Description("A list of a character's friends.")
            .Bidirectional()
            .Resolve(context => context.GetPagedResults<Droid, StarWarsCharacter>(data, context.Source.Friends));

        Field<ListGraphType<EpisodeEnum>>("appearsIn").Description("Which movie they appear in.");
        Field<StringGraphType>("primaryFunction").Description("The primary function of the droid.").Resolve(context => context.Source.PrimaryFunction);

        Interface<CharacterInterface>();
    }
}
