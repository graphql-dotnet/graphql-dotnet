using System;

namespace GraphQL.Types
{
    public class ListGraphType<T> : ListGraphType
        where T : GraphType, new()
    {
        public ListGraphType()
            : base(typeof(T))
        {
        }
    }

    public class ListGraphType : GraphType
    {
        public ListGraphType(Type type)
        {
            Type = type;
        }

        public Type Type { get; private set; }

        public GraphType CreateType()
        {
            return (GraphType)Activator.CreateInstance(Type);
        }

        public override string ToString()
        {
            return "[{0}]".ToFormat(Type);
        }
    }
}
