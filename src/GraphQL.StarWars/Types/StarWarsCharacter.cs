using System.Collections.Generic;
using GraphQl.SchemaGenerator.Attributes;
using Newtonsoft.Json;

namespace GraphQL.StarWars.Types
{
    public abstract class StarWarsCharacter
    {
        /// <summary>
        ///     The unique identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The name.
        /// </summary>
        public string Name { get; set; }

        public string[] Friends { get; set; }

        /// <summary>
        ///     Which movies they appear in.
        /// </summary>
        public HashSet<Episode> AppearsIn { get; set; }
    }

    [GraphType]
    public class Human : StarWarsCharacter
    {
        /// <summary>
        ///     The home planet of the human.
        /// </summary>
        public string HomePlanet { get; set; }
    }

    /// <summary>
    ///     A mechanical creature in the Star Wars universe.
    /// </summary>
    [GraphType]
    public class Droid : StarWarsCharacter
    {
        /// <summary>
        ///     The primary function of the droid.
        /// </summary>
        public string PrimaryFunction { get; set; }
    }
}
