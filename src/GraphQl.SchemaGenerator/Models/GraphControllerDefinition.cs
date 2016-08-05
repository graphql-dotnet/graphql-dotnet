using System.Collections.Generic;

namespace GraphQl.SchemaGenerator.Models
{
    public class GraphControllerDefinition
    {
        public IEnumerable<GraphRouteDefinition> Routes { get; private set; }
        public GraphControllerDefinition(IEnumerable<GraphRouteDefinition> routes)
        {
            Routes = routes;
        }
    }

}
