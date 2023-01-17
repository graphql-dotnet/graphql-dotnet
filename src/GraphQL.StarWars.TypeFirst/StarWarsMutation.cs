using GraphQL.StarWars.TypeFirst.Types;

namespace GraphQL.StarWars.TypeFirst;

[Name("Mutation")]
public class StarWarsMutation
{
    public static Human CreateHuman([FromServices] StarWarsData data, HumanInput human) => (Human)data.AddCharacter(human);
}
