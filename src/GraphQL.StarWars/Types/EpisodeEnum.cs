using GraphQL.Types;

namespace GraphQL.StarWars.Types
{
    public class EpisodeEnum : EnumerationGraphType
    {
        public EpisodeEnum()
        {
            Name = "Episode";
            Description = "One of the films in the Star Wars Trilogy.";
            AddValue("NEWHOPE", 4, "Released in 1977.");
            AddValue("EMPIRE", 5, "Released in 1980.");
            AddValue("JEDI", 6, "Released in 1983.");
        }
    }

    public enum Episodes
    {
        NEWHOPE = 4,
        EMPIRE = 5,
        JEDI = 6
    }
}
