using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(GraphQL.GraphiQL.Startup))]

namespace GraphQL.GraphiQL
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
        }
    }
}
