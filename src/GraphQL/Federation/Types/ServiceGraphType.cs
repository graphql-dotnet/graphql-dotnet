using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Federation.Types;

internal class ServiceGraphType : ObjectGraphType
{
    public ServiceGraphType(PrintOptions? printOptions)
    {
        Name = "_Service";

        printOptions ??= new() { IncludeFederationTypes = false };
        Field<StringGraphType>("sdl")
            .Resolve(context => context.Schema.Print(printOptions));
    }
}
