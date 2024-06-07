using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Federation.Types;

public class ServiceGraphType : ObjectGraphType
{
    public ServiceGraphType(PrintOptions? printOptions)
    {
        Name = "_Service";

        printOptions ??= new();
        Field<StringGraphType>("sdl")
            .Resolve(context => context.Schema.Print(printOptions));
    }
}
