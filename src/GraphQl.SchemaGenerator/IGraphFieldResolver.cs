using System;
using GraphQl.SchemaGenerator.Definitions;
using GraphQL.Types;

namespace GraphQl.SchemaGenerator
{
    public interface IGraphFieldResolver
    {
        object ResolveField(IServiceProvider serviceProvider,
            ResolveFieldContext context,
            GraphRouteDefinition route);
    }

}
