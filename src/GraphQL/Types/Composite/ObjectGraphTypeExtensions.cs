using GraphQL.Resolvers;

namespace GraphQL.Types
{
    /// <summary>
    /// Provides methods to add fields to output graph types.
    /// </summary>
    [Obsolete("This class will be removed in v8.")]
    public static class ObjectGraphTypeExtensions
    {
        /// <summary>
        /// Adds a field with the specified properties to a specified output graph type.
        /// </summary>
        /// <param name="obj">The graph type to add a field to.</param>
        /// <param name="name">The name of the field.</param>
        /// <param name="type">The graph type of this field.</param>
        /// <param name="description">The description of the field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. If not specified, <see cref="NameFieldResolver"/> will be used.</param>
        [Obsolete("Please use one of the Field() methods returning FieldBuilder and then methods defined on it or just use AddField() method directly. This method will be removed in v8.")]
        public static void Field(
            this IObjectGraphType obj,
            string name,
            IGraphType type,
            string? description = null,
            QueryArguments? arguments = null,
            Func<IResolveFieldContext, object?>? resolve = null)
        {
            var field = new FieldType
            {
                Name = name,
                Description = description,
                Arguments = arguments,
                ResolvedType = type,
                Resolver = resolve != null ? new FuncFieldResolver<object>(resolve) : null
            };
            obj.AddField(field);
        }

        /// <summary>
        /// Adds a field with the specified properties to a specified output graph type.
        /// </summary>
        /// <param name="obj">The graph type to add a field to.</param>
        /// <param name="name">The name of the field.</param>
        /// <param name="type">The graph type of this field.</param>
        /// <param name="description">The description of the field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. If not specified, <see cref="NameFieldResolver"/> will be used.</param>
        [Obsolete("Please use one of the Field() methods returning FieldBuilder and then methods defined on it or just use AddField() method directly. This method will be removed in v8.")]
        public static void FieldAsync(
            this IObjectGraphType obj,
            string name,
            IGraphType type,
            string? description = null,
            QueryArguments? arguments = null,
            Func<IResolveFieldContext, Task<object?>>? resolve = null)
        {
            var field = new FieldType
            {
                Name = name,
                Description = description,
                Arguments = arguments,
                ResolvedType = type,
                Resolver = resolve != null
                    ? new FuncFieldResolver<object>(context => new ValueTask<object?>(resolve(context)))
                    : null
            };
            obj.AddField(field);
        }
    }
}
