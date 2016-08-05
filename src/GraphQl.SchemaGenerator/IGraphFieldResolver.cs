using System;
using GraphQl.SchemaGenerator.Models;
using GraphQL.Types;

namespace GraphQl.SchemaGenerator
{
    public interface IGraphFieldResolver
    {
        object ResolveField(ResolveFieldContext context, FieldInformation route);
    }

}
