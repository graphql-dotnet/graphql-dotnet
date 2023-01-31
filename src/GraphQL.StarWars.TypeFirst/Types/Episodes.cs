using System.ComponentModel;

namespace GraphQL.StarWars.TypeFirst.Types;

[Name("Episode")]
[Description("One of the films in the Star Wars Trilogy.")]
public enum Episodes
{
    [Description("Released in 1977.")]
    NEWHOPE = 4,
    [Description("Released in 1980.")]
    EMPIRE = 5,
    [Description("Released in 1983.")]
    JEDI = 6
}
