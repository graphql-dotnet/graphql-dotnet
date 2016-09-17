using System;

namespace GraphQL.Types
{
    public class ListGraphType : WrappingGraphType
    {
        public ListGraphType(IGraphType type)
        {
            Type = type;
        }
    }
}
