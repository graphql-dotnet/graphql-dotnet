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

    public class ListGraphType : GraphType
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

        public override string CollectTypes(TypeCollectionContext context)
        {
            var innerType = context.ResolveType(Type);
            ResolvedType = innerType;
            var name = innerType.CollectTypes(context);
            context.AddType(name, innerType, context);
            return "[{0}]".ToFormat(name);
        }
    }
}
