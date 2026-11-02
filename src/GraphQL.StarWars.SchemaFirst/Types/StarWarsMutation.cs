namespace GraphQL.StarWars.SchemaFirst.Types;

public class StarWarsMutation
{
    public Human CreateHuman([FromServices] StarWarsData data, HumanInput human)
        => data.AddCharacter(human);
}
