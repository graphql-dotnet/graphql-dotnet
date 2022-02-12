using GraphQL.StarWars.Extensions;
using GraphQL.Types;

namespace GraphQL.StarWars.Types;

public class DroidType : ObjectGraphType<Droid>
{
    public DroidType(StarWarsData data)
    {
        Name = "Droid";
        Description = "A mechanical creature in the Star Wars universe.";

        Field<NonNullGraphType<StringGraphType>>("id", "The id of the droid.", resolve: context => context.Source.Id);
        Field<StringGraphType>("name", "The name of the droid.", resolve: context => context.Source.Name);

        Field<ListGraphType<CharacterInterface>>("friends", resolve: context => data.GetFriends(context.Source));

        Connection<CharacterInterface>()
            .Name("friendsConnection")
            .Description("A list of a character's friends.")
            .Bidirectional()
            .Resolve(context => context.GetPagedResults<Droid, StarWarsCharacter>(data, context.Source.Friends));

        Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");
        Field<StringGraphType>("primaryFunction", "The primary function of the droid.", resolve: context => context.Source.PrimaryFunction);

        Interface<CharacterInterface>();
    }
}
