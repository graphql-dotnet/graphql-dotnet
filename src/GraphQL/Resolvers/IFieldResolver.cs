using System;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public interface IFieldResolver
    {
        object Resolve(ResolveFieldContext context);

        [Obsolete]
        bool RunThreaded();
    }

    public interface IFieldResolver<out T> : IFieldResolver
    {
        new T Resolve(ResolveFieldContext context);
    }
}
