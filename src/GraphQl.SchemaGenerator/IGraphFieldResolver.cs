using System;
using GraphQl.SchemaGenerator.Models;
using GraphQL.Types;

namespace GraphQl.SchemaGenerator
{
    /// <summary>
    ///     The field resolver executes and returns the data requested by graph.
    /// </summary>
    public interface IGraphFieldResolver
    {
        object ResolveField(ResolveFieldContext context, FieldInformation route);
    }

}
