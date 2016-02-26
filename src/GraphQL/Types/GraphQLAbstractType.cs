using System;
using GraphQL.Execution;

namespace GraphQL.Types
{
    public abstract class GraphQLAbstractType : GraphType
    {
        public Func<object, ObjectGraphType> ResolveType { get; set; }

        public abstract bool IsPossibleType(ExecutionContext context, GraphType type);
    }
}
