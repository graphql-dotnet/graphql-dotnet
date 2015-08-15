using System;

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
            Type = type;
        }

        public string Name { get; set; }

        public object DefaultValue { get; set; }

        public Type Type { get; private set; }
    }
}
