namespace GraphQL.StarWars.TypeFirst.Types;

[Implements(typeof(IStarWarsCharacter))]
public class Human : StarWarsCharacter
{
    public string? HomePlanet { get; set; }
}
