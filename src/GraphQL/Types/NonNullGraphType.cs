using System;

namespace GraphQL.Types
{
    public class NonNullGraphType<T> : NonNullGraphType
        where T : GraphType, new()
    {
        public NonNullGraphType()
            : base(new T())
        {
        }
    }

    public class NonNullGraphType : GraphType
    {
        public static readonly NonNullGraphType String = new NonNullGraphType(new StringGraphType());
        public static readonly NonNullGraphType Boolean = new NonNullGraphType(new BooleanGraphType());
        public static readonly NonNullGraphType Int = new NonNullGraphType(new IntGraphType());
        public static readonly NonNullGraphType Float = new NonNullGraphType(new FloatGraphType());
        public static readonly NonNullGraphType Id = new NonNullGraphType(new IdGraphType());

        public NonNullGraphType(GraphType type)
        {
            if (type.GetType() == typeof (NonNullGraphType))
            {
                throw new ArgumentException("Cannot nest NonNull inside NonNull.", "type");
            }

            Type = type;
        }

        public GraphType Type { get; private set; }

        public override string ToString()
        {
            return "{0}!".ToFormat(Type);
        }
    }
}
