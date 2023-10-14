namespace GraphQL.Types
{
    /// <summary>
    /// An interface for such graph types that do not represent concrete graph types, that is, for interfaces and unions.
    /// </summary>
    public interface IAbstractGraphType : IGraphType
    {
        /// <summary>
        /// Gets or sets a delegate that can be used to determine the proper graph type for the specified object value. See
        /// <see cref="AbstractGraphTypeExtensions.GetObjectType(IAbstractGraphType, object, ISchema)"/> for more details.
        /// </summary>
        Func<object, IObjectGraphType?>? ResolveType { get; set; }

        /// <summary>
        /// Returns a set of possible types for this abstract graph type.
        /// </summary>
        PossibleTypes PossibleTypes { get; }

        /// <summary>
        /// Adds the specified graph type to a list of possible graph types for this abstract graph type.
        /// </summary>
        void AddPossibleType(IObjectGraphType type);
    }

    /// <summary>
    /// Provides extension methods for <see cref="IAbstractGraphType"/> instances.
    /// </summary>
    public static class AbstractGraphTypeExtensions
    {
        /// <summary>
        /// Returns true if the specified graph type is one of the possible graph types for this abstract graph type.
        /// </summary>
        public static bool IsPossibleType(this IAbstractGraphType abstractType, IGraphType type)
        {
            foreach (var possible in abstractType.PossibleTypes.List)
            {
                if (possible.Equals(type))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the proper graph type for the specified object for this abstract graph type. If the abstract
        /// graph type implements <see cref="IAbstractGraphType.ResolveType"/>, then this method is called to determine
        /// the best graph type to use. Otherwise, <see cref="IObjectGraphType.IsTypeOf"/> is called on each possible
        /// graph type supported by the abstract graph type to determine if a match can be found.
        /// </summary>
        public static IObjectGraphType? GetObjectType(this IAbstractGraphType abstractType, object value, ISchema schema)
        {
            var result = abstractType.ResolveType != null
                ? abstractType.ResolveType(value)
                : GetTypeOf(abstractType, value);

            if (result is GraphQLTypeReference reference)
                result = schema.AllTypes[reference.TypeName] as IObjectGraphType;

            return result;

            static IObjectGraphType? GetTypeOf(IAbstractGraphType abstractType, object value)
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
}
