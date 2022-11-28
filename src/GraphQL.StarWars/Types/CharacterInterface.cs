using GraphQL.Types;
using GraphQL.Types.Relay;

namespace GraphQL.StarWars.Types;

public class CharacterInterface : InterfaceGraphType<StarWarsCharacter>
{
    public CharacterInterface()
    {
        Name = "Character";

        Field<NonNullGraphType<StringGraphType>>("id")
            .Description("The id of the character.")
            .Resolve(context => context.Source.Id);

        Field<StringGraphType>("name")
            .Description("The name of the character.")
            .Resolve(context => context.Source.Name);

        Field<ListGraphType<CharacterInterface>>("friends");
        Field<ConnectionType<CharacterInterface, EdgeType<CharacterInterface>>>("friendsConnection");

        Field<ListGraphType<EpisodeEnum>>("appearsIn")
            .Description("Which movie they appear in.");
    }
}
