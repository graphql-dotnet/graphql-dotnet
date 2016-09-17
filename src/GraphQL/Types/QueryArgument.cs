using System;

namespace GraphQL.Types
{
    public class QueryArgument<TType> : QueryArgument
        where TType : GraphType
    {
        public QueryArgument()
            : base(typeof(TType).GetGraphTypeFromType)
        {
        }
    }

    public class QueryArgument : IHaveDefaultValue
    {
        public QueryArgument(IGraphType type)
        {
            Type = type;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public object DefaultValue { get; set; }

        public IGraphType Type { get; private set; }
    }
}
