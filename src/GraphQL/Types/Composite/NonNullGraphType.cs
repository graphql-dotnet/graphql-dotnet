using System;

namespace GraphQL.Types
{
    public class NonNullGraphType<T> : NonNullGraphType
        where T : GraphType
    {
        public NonNullGraphType()
            : base(typeof(T))
        {
        }
    }

    public class NonNullGraphType : GraphType
    {
        public NonNullGraphType(GraphType type)
        {
            if (type is NonNullGraphType)
            {
                throw new ArgumentException("Cannot nest NonNull inside NonNull.", nameof(type));
            }

            ResolvedType = type;
        }

        protected NonNullGraphType(Type type)
        {
            if (type == typeof (NonNullGraphType))
            {
                throw new ArgumentException("Cannot nest NonNull inside NonNull.", nameof(type));
            }

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
            return "{0}!".ToFormat(name);
        }

        public override string ToString() => $"{ResolvedType}!";
    }
}
