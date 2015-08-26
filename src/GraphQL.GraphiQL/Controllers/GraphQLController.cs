using GraphQL.Tests;
using GraphQL.Types;
using System.Web.Http;

namespace GraphQL.GraphiQL.Controllers
{
    public class GraphQLController : ApiController
    {
        private Schema _schema;

        public GraphQLController()
        {
            _schema = new StarWarsSchema();
        }

        public ExecutionResult Post(GraphQLQuery query)
        {
            return Execute(_schema, null, query.Query);
        }

        public ExecutionResult Execute(
          Schema schema,
          object rootObject,
          string query,
          string operationName = null,
          Inputs inputs = null)
        {
            var executer = new DocumentExecuter();
            return executer.Execute(schema, rootObject, query, operationName);
        }
    }

    public class GraphQLQuery
    {
        public string Query { get; set; }
        public string Variables { get; set; }
    }
}
