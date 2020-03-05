using System;

namespace GraphQL.Types
{
    public class ListGraphType<T> : ListGraphType
        where T : GraphType
    {
        public ListGraphType()
            : base(typeof(T))
        {
        }
    }

    public class ListGraphType : GraphType
    {
        public ListGraphType(GraphType type)
        {
            ResolvedType = type;
        }

        protected ListGraphType(Type type)
        {
            Type = type;
        }

        public Type Type { get; private set; }

        public GraphType ResolvedType { get; set; }

        public override string CollectTypes(TypeCollectionContext context)
        {
            var innerType = context.ResolveType(Type);
            ResolvedType = (GraphType)innerType; //ugly hack
            var name = ResolvedType.CollectTypes(context);
            context.AddType(name, innerType, context);
            return "[{0}]".ToFormat(name);
        }

        public override string ToString() => $"[{ResolvedType}]";
    }
}
