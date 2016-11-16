using System;
using System.Reflection;

namespace GraphQL.Types
{
    public class QueryArgument<TType> : QueryArgument
        where TType : GraphType
    {
        public QueryArgument()
            : base(typeof(TType))
        {
        }
    }

    public class QueryArgument : IHaveDefaultValue
    {
        public QueryArgument(Type type)
        {
            if (type == null || !typeof(GraphType).IsAssignableFrom(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "QueryArgument type is required and must derive from GraphType.");
            }

            Type = type;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public object DefaultValue { get; set; }

        public IGraphType ResolvedType { get; set; }
        public Type Type { get; private set; }
    }
}
