#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using GraphQL.Types;

namespace GraphQL.Utilities.Federation
{
    public class ServiceGraphType : ObjectGraphType
    {
        public ServiceGraphType()
        {
            Name = "_Service";

            Field<StringGraphType>("sdl").Resolve(context =>
            {
                var printer = new FederatedSchemaPrinter(context.Schema);
                return printer.PrintFederatedSchema();
            });
        }
    }
}
