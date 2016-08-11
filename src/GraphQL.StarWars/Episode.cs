using GraphQL.SchemaGenerator.Attributes;

namespace GraphQL.StarWars
{
    [GraphType]
    public enum Episode
    {
        /// <summary>
        ///     Released in 1977.
        /// </summary>
        NEWHOPE  = 4,

        /// <summary>
        ///     Released in 1980.
        /// </summary>
        EMPIRE  = 5,

        /// <summary>
        ///     Released in 1983.
        /// </summary>
        JEDI  = 6
    }
}