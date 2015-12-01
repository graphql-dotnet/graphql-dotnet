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

    public abstract class NonNullGraphType : GraphType
    {
        protected NonNullGraphType(Type type)
        {
            if (type == typeof (NonNullGraphType))
            {
                throw new ArgumentException("Cannot nest NonNull inside NonNull.", "type");
            }

            Type = type;
        }

        public Type Type { get; private set; }

        public override string CollectTypes(TypeCollectionContext context)
        {
            var innerType = context.ResolveType(Type);
            var name = innerType.CollectTypes(context);
            context.AddType(name, innerType, context);
            return "{0}!".ToFormat(name);
        }
    }
}
