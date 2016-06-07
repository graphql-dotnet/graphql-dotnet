using GraphQL.Types;

namespace GraphQL
{
    public static class GraphQLExtensions
    {
        public static bool IsLeafType(this GraphType type, ISchema schema)
        {
            var namedType = type.GetNamedType(schema);
            return namedType is ScalarGraphType || namedType is EnumerationGraphType;
        }

        public static GraphType GetNamedType(this GraphType type, ISchema schema)
        {
            GraphType unmodifiedType = type;

            if (type is NonNullGraphType)
            {
                var nonNull = (NonNullGraphType) type;
                return GetNamedType(schema.FindType(nonNull.Type), schema);
            }

            if (type is ListGraphType)
            {
                var list = (ListGraphType) type;
                return GetNamedType(schema.FindType(list.Type), schema);
            }

            return unmodifiedType;
        }
    }
}
