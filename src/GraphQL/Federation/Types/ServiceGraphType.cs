using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Federation.Types;

/// <summary>
/// Represents an object type for services in GraphQL Federation.
/// Used to expose the SDL (Schema Definition Language) of a federation subgraph.
/// The name of this graph type is "_Service".
/// </summary>
public class ServiceGraphType : ObjectGraphType
{
    /// <inheritdoc cref="ServiceGraphType"/>
    /// <param name="printOptions">Optional print options for schema printing.</param>
    public ServiceGraphType(PrintOptions? printOptions)
    {
        Name = "_Service";

        printOptions ??= new();
        Field<StringGraphType>("sdl")
            .Resolve(context => context.Schema.Print(printOptions));
    }
}
