using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.Resolvers
{
    public interface IFieldResolver
    {
        object Resolve(ResolveFieldContext context);
    }

    public interface IFieldResolver<out T> : IFieldResolver
    {
        new T Resolve(ResolveFieldContext context);
    }
}
