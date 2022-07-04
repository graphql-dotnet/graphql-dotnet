using GraphQL.StarWars.Extensions;
using GraphQL.Types;

namespace GraphQL.StarWars.Types;

public class HumanType : ObjectGraphType<Human>
{
    public HumanType(StarWarsData data)
    {
        Name = "Human";

        Field<NonNullGraphType<StringGraphType>>("id", "The id of the human.", resolve: context => context.Source.Id);
        Field<StringGraphType>("name", "The name of the human.", resolve: context => context.Source.Name);

        Field<ListGraphType<CharacterInterface>>("friends", resolve: context => data.GetFriends(context.Source));

        Connection<CharacterInterface>()
            .Name("friendsConnection")
            .Description("A list of a character's friends.")
            .Bidirectional()
            .Resolve(context => context.GetPagedResults<Human, StarWarsCharacter>(data, context.Source.Friends));

        Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");

        Field<StringGraphType>("homePlanet", "The home planet of the human.", resolve: context => context.Source.HomePlanet);

        Interface<CharacterInterface>();
    }
}
