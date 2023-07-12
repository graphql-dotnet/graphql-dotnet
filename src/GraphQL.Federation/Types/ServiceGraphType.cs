using GraphQL.Federation.Extensions;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Federation.Types;

internal class ServiceGraphType : ObjectGraphType
{
    public ServiceGraphType(SchemaPrinterOptions? schemaPrinterOptions)
    {
        Name = "_Service";

        Field<StringGraphType>("sdl")
            .Resolve(context =>
            {
                var printer = new FederatedSchemaPrinter(context.Schema, schemaPrinterOptions);
                return printer.PrintFederatedSchema();
            });
    }
}
