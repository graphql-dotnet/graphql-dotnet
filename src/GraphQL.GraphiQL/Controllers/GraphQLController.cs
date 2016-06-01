using GraphQL.Tests;
using GraphQL.Types;
using System.Threading.Tasks;
using System.Web.Http;

namespace GraphQL.GraphiQL.Controllers
{
    public class GraphQLController : ApiController
    {
        private readonly ISimpleContainer _container;
        private readonly ISchema _schema;
        private readonly IDocumentExecuter _executer;

        public GraphQLController()
        {
            _executer = new DocumentExecuter();

            _container = new SimpleContainer();
            _container.Singleton(new StarWarsData());
            _container.Register<StarWarsQuery>();
            _container.Register<HumanType>();
            _container.Register<DroidType>();
            _container.Register<CharacterInterface>();
            _container.Singleton(() => new StarWarsSchema(type => (GraphType) _container.Get(type)));

            _schema = _container.Get<StarWarsSchema>();
        }

        public async Task<ExecutionResult> Post(GraphQLQuery query)
        {
            var inputs = query.Variables.ToInputs();
            return await Execute(_schema, null, query.Query, query.OperationName, inputs);
        }

        public async Task<ExecutionResult> Execute(
          ISchema schema,
          object rootObject,
          string query,
          string operationName = null,
          Inputs inputs = null)
        {
            return await _executer.ExecuteAsync(schema, rootObject, query, operationName, inputs);
        }
    }

    public class GraphQLQuery
    {
        public string OperationName { get; set; }
        public string Query { get; set; }
        public string Variables { get; set; }
    }
}
