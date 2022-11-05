#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using GraphQL.Types;

namespace GraphQL.Utilities.Federation
{
    /// <summary>
    /// Schema composition at the gateway requires having each service's schema, annotated
    /// with its federation configuration. This information is fetched from each service
    /// using _service field of type _Service, an enhanced introspection entry point added
    /// to the query root of each federated service.
    /// <br/>
    /// <see href="https://www.apollographql.com/docs/federation/federation-spec/#type-_service"/>
    /// </summary>
    public class ServiceGraphType : ObjectGraphType
    {
        public ServiceGraphType()
        {
            Name = "_Service";

            Field<StringGraphType>("sdl").Description("SDL of the service's schema").Resolve(context =>
            {
                var printer = new FederatedSchemaPrinter(context.Schema);
                return printer.PrintFederatedSchema();
            });
        }
    }
}
