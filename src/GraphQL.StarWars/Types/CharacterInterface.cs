using GraphQL.Types;

namespace GraphQL.StarWars.Types
{
    public class CharacterInterface : InterfaceGraphType<StarWarsCharacter>
    {
        public CharacterInterface()
        {
            Name = "Character";

            Field(d => d.Id).Description("The id of the character.");
            Field(d => d.Name, nullable: true).Description("The name of the character.");

            Field<ListGraphType<CharacterInterface>>("friends");
            Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");
        }
    }
}
