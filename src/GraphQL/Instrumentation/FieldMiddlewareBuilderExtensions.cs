using GraphQL.Types;
using System;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Extension methods for <see cref="IFieldMiddlewareBuilder"/> to add middlewares.
    /// These methods are built on top of <see cref="IFieldMiddlewareBuilder.Use(Func{ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate})"/>.
    /// </summary>
    public static class FieldMiddlewareBuilderExtensions
    {
        /// <summary>
        /// Adds middleware to the list of delegates that will be applied to the schema when invoking <see cref="IFieldMiddlewareBuilder.ApplyTo(ISchema)"/>.
        /// </summary>
        /// <param name="builder">Interface for connecting middlewares to a schema.</param>
        /// <param name="middleware">Middleware instance.</param>
        /// <returns>Reference to the same <see cref="IFieldMiddlewareBuilder"/>.</returns>
        public static IFieldMiddlewareBuilder Use(this IFieldMiddlewareBuilder builder, IFieldMiddleware middleware)
            => builder.Use(next => context => middleware.Resolve(context, next));

        /// <summary>
        /// Adds the specified delegate to the list of delegates that will be applied to the schema when invoking <see cref="IFieldMiddlewareBuilder.ApplyTo(ISchema)"/>.
        /// <br/><br/>
        /// This is a compatibility shim when compiling delegates without schema specified.
        /// </summary>
        /// <param name="builder">Interface for connecting middlewares to a schema.</param>
        /// <param name="middleware">Middleware delegate.</param>
        /// <returns>Reference to the same <see cref="IFieldMiddlewareBuilder"/>.</returns>
        public static IFieldMiddlewareBuilder Use(this IFieldMiddlewareBuilder builder, Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate> middleware)
            => builder.Use((_, next) => middleware(next));

        /// <summary>
        /// Adds middleware specified by its type to the list of delegates that will be applied to the schema when invoking <see cref="IFieldMiddlewareBuilder.ApplyTo(ISchema)"/>.
        /// <br/><br/>
        /// Middleware will be created using the DI container obtained from the <see cref="Schema"/>.
        /// </summary>
        /// <typeparam name="T">Middleware type.</typeparam>
        /// <param name="builder">Interface for connecting middlewares to a schema.</param>
        /// <returns>Reference to the same <see cref="IFieldMiddlewareBuilder"/>.</returns>
        public static IFieldMiddlewareBuilder Use<T>(this IFieldMiddlewareBuilder builder) where T : IFieldMiddleware => Use(builder, typeof(T));

        /// <summary>
        /// Adds middleware specified by its type to the list of delegates that will be applied to the schema when invoking <see cref="IFieldMiddlewareBuilder.ApplyTo(ISchema)"/>.
        /// <br/><br/>
        /// Middleware will be created using the DI container obtained from the <see cref="Schema"/>.
        /// </summary>
        /// <param name="builder">Interface for connecting middlewares to a schema.</param>
        /// <param name="middleware">Middleware type.</param>
        /// <returns>Reference to the same <see cref="IFieldMiddlewareBuilder"/>.</returns>
        public static IFieldMiddlewareBuilder Use(this IFieldMiddlewareBuilder builder, System.Type middleware)
        {
            if (!typeof(IFieldMiddleware).IsAssignableFrom(middleware))
                throw new ArgumentException($"Field middleware of type '{middleware.FullName}' must implement the {nameof(IFieldMiddleware)} interface", nameof(middleware));

            return builder.Use((schema, next) =>
            {
                if (schema == null)
                    throw new InvalidOperationException("Schema is null. Schema required for resolving middlewares from DI container.");

                // Not an ideal solution, but at least it allows to work with custom schemas which are not inherited from Schema type
                if (!(schema is IServiceProvider provider))
                    throw new NotSupportedException($"'{schema.GetType().FullName}' should implement 'IServiceProvider' interface for resolving middlewares.");

                var instance = (IFieldMiddleware)provider.GetService(middleware);
                if (instance == null)
                    throw new InvalidOperationException($"Field middleware of type '{middleware.FullName}' must be registered in the DI container.");

                return context => instance.Resolve(context, next);
            });
        }
    }
}
