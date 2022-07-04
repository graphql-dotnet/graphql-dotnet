using GraphQL.Types;
using GraphQL.Types.Relay;

namespace GraphQL.StarWars.Types;

public class CharacterInterface : InterfaceGraphType<StarWarsCharacter>
{
    public CharacterInterface()
    {
        Name = "Character";

        Field<NonNullGraphType<StringGraphType>>("id", "The id of the character.", resolve: context => context.Source.Id);
        Field<StringGraphType>("name", "The name of the character.", resolve: context => context.Source.Name);

        Field<ListGraphType<CharacterInterface>>("friends");
        Field<ConnectionType<CharacterInterface, EdgeType<CharacterInterface>>>("friendsConnection");
        Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");
    }
}
