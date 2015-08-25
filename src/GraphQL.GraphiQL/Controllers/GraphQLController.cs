using GraphQL.Tests;
using GraphQL.Types;
using System.Web.Http;
using GraphQL.Introspection;

namespace GraphQL.GraphiQL.Controllers
{
    public class GraphQLController : ApiController
    {
        private Schema _schema;

        public GraphQLController()
        {
            _schema = new StarWarsSchema();
        }

        public ExecutionResult Get()
        {
            return Execute(_schema, null, "{ hero { __typename name } }");
        }

        public ExecutionResult Post()
        {
            return Execute(_schema, null, SchemaIntrospection.IntrospectionQuery);
        }

        public ExecutionResult Post(string query)
        {
            return Execute(_schema, null, query);
        }

        public ExecutionResult Execute(
          Schema schema,
          object rootObject,
          string query,
          string operationName = null,
          Inputs inputs = null)
        {
            var executer = new DocumentExecuter();

            var result = executer.Execute(schema, rootObject, query, operationName);
            return result;
        }
    }
}
