using System;

namespace GraphQL.Types
{
    /// <summary>
    /// An interface for such graph types that do not represent concrete graph types, that is, for interfaces and unions. 
    /// </summary>
    public interface IAbstractGraphType : IGraphType
    {
        Func<object, IObjectGraphType> ResolveType { get; set; }

        /// <summary>
        /// Returns a set of possible types for this abstract graph type.
        /// </summary>
        PossibleTypes PossibleTypes { get; }

        void AddPossibleType(IObjectGraphType type);
    }

    public static class AbstractGraphTypeExtensions
    {
        public static bool IsPossibleType(this IAbstractGraphType abstractType, IGraphType type)
        {
            foreach (var possible in abstractType.PossibleTypes.List)
            {
                if (possible.Equals(type))
                    return true;
            }

            return false;
        }

        public static IObjectGraphType GetObjectType(this IAbstractGraphType abstractType, object value, ISchema schema)
        {
            var result = abstractType.ResolveType != null
                ? abstractType.ResolveType(value)
                : GetTypeOf(abstractType, value);

            if (result is GraphQLTypeReference reference)
                result = schema.AllTypes[reference.TypeName] as IObjectGraphType;

            return result;
        }

        public static IObjectGraphType GetTypeOf(this IAbstractGraphType abstractType, object value)
        {
            foreach (var possible in abstractType.PossibleTypes.List)
            {
                if (possible.IsTypeOf != null && possible.IsTypeOf(value))
                    return possible;
            }

            return null;
        }
    }
}
