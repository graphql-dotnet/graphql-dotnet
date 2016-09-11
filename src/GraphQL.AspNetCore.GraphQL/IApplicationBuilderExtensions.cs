using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace GraphQL.AspNetCore.GraphQL {
    /// <summary>
    ///     Extension methods for using GraphQL with <see cref="IApplicationBuilder" />.
    /// </summary>
    public static class IApplicationBuilderExtensions {
        /// <summary>
        ///     Uses GraphiQL with the specified options.
        /// </summary>
        /// <param name="app">
        ///     The application builder.
        /// </param>
        /// <param name="options">
        ///     The options for the GraphiQL middleware.
        /// </param>
        /// <returns>
        ///     The modified application builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="app" /> or <paramref name="options" /> is null.
        /// </exception>
        public static IApplicationBuilder UseGraphQL(this IApplicationBuilder app, GraphQLOptions options) {
            if(app == null) {
                throw new ArgumentNullException(nameof(app));
            }
            if(options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            return app.UseMiddleware<GraphQLMiddleware>(Options.Create(options));
        }
    }
}
