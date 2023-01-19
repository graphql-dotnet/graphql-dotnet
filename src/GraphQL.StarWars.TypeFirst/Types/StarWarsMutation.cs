namespace GraphQL.StarWars.TypeFirst.Types;

[Name("Mutation")]
public class StarWarsMutation
{
    public static Human CreateHuman([FromServices] StarWarsData data, HumanInput human) => (Human)data.AddCharacter(human);
}
