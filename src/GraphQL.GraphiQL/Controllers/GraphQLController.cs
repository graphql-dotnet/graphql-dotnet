using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using GraphQL.Http;
using GraphQL.StarWars;
using GraphQL.Types;

namespace GraphQL.GraphiQL.Controllers
{
    public class GraphQLController : ApiController
    {
        private readonly ISchema _schema;
        private readonly IDocumentExecuter _executer;
        private readonly IDocumentWriter _writer;

        public GraphQLController(
            IDocumentExecuter executer,
            IDocumentWriter writer,
            StarWarsSchema schema)
        {
            _executer = executer;
            _writer = writer;
            _schema = schema;
        }

        // This will display an example error
        public async Task<HttpResponseMessage> Get(HttpRequestMessage request)
        {
            return await Post(request, new GraphQLQuery { Query = "query foo { hero }", Variables = "" });
        }

        public async Task<HttpResponseMessage> Post(HttpRequestMessage request, GraphQLQuery query)
        {
            var inputs = query.Variables.ToInputs();
            var result = await Execute(_schema, null, query.Query, query.OperationName, inputs);

            var httpResult = result.Errors?.Count > 0
                ? HttpStatusCode.BadRequest
                : HttpStatusCode.OK;

            var json = _writer.Write(result);

            var response = request.CreateResponse(httpResult);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return response;
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
