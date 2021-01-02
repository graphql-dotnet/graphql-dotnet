using System;

namespace GraphQL.Types
{
    public class ListGraphType<T> : ListGraphType
        where T : IGraphType
    {
        public ListGraphType()
            : base(typeof(T))
        {
        }
    }

    public class ListGraphType : GraphType, IProvideResolvedType
    {
        public ListGraphType(IGraphType type)
        {
            ResolvedType = type;
        }

        protected ListGraphType(Type type)
        {
            Type = type;
        }

        public Type Type { get; private set; }

        public IGraphType ResolvedType { get; set; }

        public override string ToString() => $"[{ResolvedType}]";
    }
}
