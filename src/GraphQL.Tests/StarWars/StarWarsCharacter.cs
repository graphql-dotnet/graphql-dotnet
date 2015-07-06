namespace GraphQL.Tests
{
    public abstract class StarWarsCharacter
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string[] Friends { get; set; }
    }

    public class Human : StarWarsCharacter
    {
    }

    public class Droid : StarWarsCharacter
    {
    }
}