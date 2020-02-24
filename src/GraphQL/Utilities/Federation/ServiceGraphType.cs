using GraphQL.Types;

namespace GraphQL.Utilities.Federation
{
    public class ServiceGraphType : ObjectGraphType
    {
        public ServiceGraphType()
        {
            Name = "_Service";

            Field<StringGraphType>("sdl", resolve: context =>
            {
                var printer = new FederatedSchemaPrinter(context.Schema);
                return printer.PrintFederatedSchema();
            });
        }
    }
}
