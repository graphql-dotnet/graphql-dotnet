#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using GraphQL.Types;

namespace GraphQL.Utilities.Federation;

public class ServiceGraphType : ObjectGraphType
{
    public ServiceGraphType()
    {
        Name = "_Service";

        var options = new PrintOptions
        {
            IncludeFederationTypes = false, // for federation v1 support
        };
        Field<StringGraphType>("sdl").Resolve(context => context.Schema.Print(options));
    }
}
