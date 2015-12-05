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

    public abstract class ListGraphType : GraphType
    {
        protected ListGraphType(Type type)
        {
            Type = type;
        }

        public Type Type { get; private set; }

        public override string CollectTypes(TypeCollectionContext context)
        {
            var innerType = context.ResolveType(Type);
            var name = innerType.CollectTypes(context);
            context.AddType(name, innerType, context);
            return "[{0}]".ToFormat(name);
        }
    }
}
