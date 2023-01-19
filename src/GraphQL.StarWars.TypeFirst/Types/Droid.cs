namespace GraphQL.StarWars.TypeFirst.Types;

[Implements(typeof(IStarWarsCharacter))]
public class Droid : StarWarsCharacter
{
    public string? PrimaryFunction { get; set; }
}
