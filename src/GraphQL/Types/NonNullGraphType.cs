using System;

namespace GraphQL.Types
{

    public class NonNullGraphType : WrappingGraphType
    {
        public NonNullGraphType(IGraphType type)
        {
            if (type is NonNullGraphType)
            {
                throw new ArgumentException("Cannot nest NonNull inside NonNull.", "type");
            }

            Type = type;
        }
    }
}
