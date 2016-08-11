using System.Threading.Tasks;
using GraphQL.SchemaGenerator.Attributes;
using GraphQL.StarWars.Types;

namespace GraphQL.StarWars
{
    /// <summary>
    ///     An example of the sdk that could be exposed. This is decorated with attributes to self generate a graph schema. 
    /// </summary>
    [GraphType]
    public class StarWars
    {
        private readonly StarWarsData _data = new StarWarsData();

        /// <summary>
        ///     Get the current hero.
        /// </summary>
        /// <remarks>
        ///     Example of graph ql attribute using the defaults.
        /// </remarks>
        /// <returns>Droid.</returns>
        [GraphRoute]
        public Droid Hero()
        {
            var item = _data.GetDroidByIdAsync("3").Result;

            return item;
        }

        /// <summary>
        ///     Get human by id.
        /// </summary>
        /// <remarks>Async is supported.</remarks>
        /// <param name="id">id of the droid</param>
        /// <returns></returns>
        [GraphRoute("human")]
        public Task<Human> GetHumanByIdAsync(string id)
        {
            var item = _data.GetHumanByIdAsync(id);

            return item;
        }

        /// <summary>
        ///     Get 
        /// </summary>
        /// <param name="id">id of the human</param>
        /// <returns></returns>
        [GraphRoute("droid", false)]
        public Task<Droid> GetDroidByIdAsync(string id)
        {
            var item = _data.GetDroidByIdAsync(id);

            return item;
        }
    }
}
