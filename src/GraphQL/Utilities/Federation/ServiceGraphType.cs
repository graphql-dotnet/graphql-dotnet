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
#pragma warning disable CS0618 // Type or member is obsolete
                var printer = new FederatedSchemaPrinter(context.Schema);
#pragma warning restore CS0618 // Type or member is obsolete
                return printer.PrintFederatedSchema();
            });
        }
    }
}
