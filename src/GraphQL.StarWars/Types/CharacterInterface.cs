using GraphQL.Types;
using GraphQL.Types.Relay;

namespace GraphQL.StarWars.Types;

public class CharacterInterface : InterfaceGraphType<StarWarsCharacter>
{
    public CharacterInterface()
    {
        Name = "Character";

        Field<NonNullGraphType<StringGraphType>>("id")
            .Description("The id of the character.");

        Field<StringGraphType>("name")
            .Description("The name of the character.");

        Field<ListGraphType<CharacterInterface>>("friends");
        Field<ConnectionType<CharacterInterface, EdgeType<CharacterInterface>>>("friendsConnection");

        Field<ListGraphType<EpisodeEnum>>("appearsIn")
            .Description("Which movie they appear in.");
    }
}
