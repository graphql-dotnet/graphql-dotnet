using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Interface for connecting middlewares to a schema.
    /// </summary>
    public interface IFieldMiddlewareBuilder
    {
        /// <summary>
        /// Adds the specified delegate to the list of delegates that will be applied to all field resolvers when invoking <see cref="SchemaTypes.ApplyMiddleware(IFieldMiddlewareBuilder)"/>.
        /// <br/><br/>
        /// The delegate is used to unify the different ways of specifying middleware. See additional methods in <see cref="FieldMiddlewareBuilderExtensions"/>.
        /// </summary>
        /// <param name="middleware">Middleware delegate.</param>
        /// <returns>Reference to the same <see cref="IFieldMiddlewareBuilder"/>.</returns>
        IFieldMiddlewareBuilder Use(Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate> middleware);

        /// <summary>
        /// Returns a transform for field resolvers, or <see langword="null"/> if no middleware is defined.
        /// The transform is a cumulation of all middlewares configured within this builder.
        /// </summary>
        Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate>? Build();
    }
}
