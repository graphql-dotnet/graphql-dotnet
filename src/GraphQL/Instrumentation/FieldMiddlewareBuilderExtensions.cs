using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Extension methods for <see cref="IFieldMiddlewareBuilder"/> to add middlewares.
    /// These methods are built on top of <see cref="IFieldMiddlewareBuilder.Use(Func{FieldMiddlewareDelegate, FieldMiddlewareDelegate})"/>.
    /// </summary>
    public static class FieldMiddlewareBuilderExtensions
    {
        /// <summary>
        /// Adds middleware to the list of delegates that will be applied to all field resolvers when invoking <see cref="SchemaTypes.ApplyMiddleware(IFieldMiddlewareBuilder)"/>.
        /// </summary>
        /// <param name="builder">Interface for connecting middlewares to a schema.</param>
        /// <param name="middleware">Middleware instance.</param>
        /// <returns>Reference to the same <see cref="IFieldMiddlewareBuilder"/>.</returns>
        public static IFieldMiddlewareBuilder Use(this IFieldMiddlewareBuilder builder, IFieldMiddleware middleware)
            => builder.Use(next => context => middleware.ResolveAsync(context, next));
    }
}
