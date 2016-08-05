using System.Threading.Tasks;
using System.Web.Http;
using GraphQl.SchemaGenerator.Attributes;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;

namespace GraphQl.StarWars.Api.Controllers
{
    public class StarWarsController : ApiController
    {
        [GraphRoute("hero", typeof(Droid))]
        [HttpGet]
        [Route("hero")]
        public async Task<IHttpActionResult> Get()
        {
            var data = new StarWarsData();

            var item = await data.GetDroidByIdAsync("3");

            return Ok(item);
        }

        /// <summary>
        ///     Get human.
        /// </summary>
        /// <param name="id">id of the human</param>
        /// <returns></returns>
        [GraphRoute("human", typeof(Human))]
        [HttpGet]
        [Route("human")]
        public async Task<IHttpActionResult> GetHuman(string id)
        {
            var data = new StarWarsData();

            var item = await data.GetHumanByIdAsync(id);

            return Ok(item);
        }

        /// <summary>
        ///     Get droid.
        /// </summary>
        /// <param name="id">id of the droid</param>
        /// <returns></returns>
        [GraphRoute("driod", typeof(Droid))]
        [HttpGet]
        [Route("driod")]
        public async Task<IHttpActionResult> GetDriod(string id)
        {
            var data = new StarWarsData();

            var item = await data.GetDroidByIdAsync(id);

            return Ok(item);
        }
    }
}
