using System;
using System.Diagnostics;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    public class QueryArgument<TType> : QueryArgument
        where TType : IGraphType
    {
        public QueryArgument()
            : base(typeof(TType))
        {
        }
    }

    [DebuggerDisplay("{Name,nq}: {ResolvedType,nq}")]
    public class QueryArgument : MetadataProvider, IHaveDefaultValue
    {
        public QueryArgument(IGraphType type)
        {
            ResolvedType = type ?? throw new ArgumentOutOfRangeException(nameof(type), "QueryArgument type is required");
        }

        public QueryArgument(Type type)
        {
            if (type == null || !typeof(IGraphType).IsAssignableFrom(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "QueryArgument type is required and must derive from IGraphType.");
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
