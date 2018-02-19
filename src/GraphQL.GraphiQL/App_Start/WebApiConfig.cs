using System.Web.Http;

namespace GraphQL.GraphiQL
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            var bootstrapper = new Bootstrapper();
            config.DependencyResolver = bootstrapper.Resolver();

            // Web API routes
            config.MapHttpAttributeRoutes();
            //enable cors
            config.EnableCors();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
