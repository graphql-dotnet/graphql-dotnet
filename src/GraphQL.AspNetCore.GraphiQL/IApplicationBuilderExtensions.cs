using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace GraphQL.AspNetCore.GraphiQL {
    /// <summary>
    ///     Extension methods for using GraphiQL with <see cref="IApplicationBuilder" />.
    /// </summary>
    public static class IApplicationBuilderExtensions {
        /// <summary>
        ///     Uses GraphiQL with default options.
        /// </summary>
        /// <param name="app">
        ///     The application builder.
        /// </param>
        /// <returns>
        ///     The modified application builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="app" /> is null.
        /// </exception>
        public static IApplicationBuilder UseGraphiQL(this IApplicationBuilder app) {
            if(app == null) {
                throw new ArgumentNullException(nameof(app));
            }
            return app.UseMiddleware<GraphiQLMiddleware>();
        }

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
        public static IApplicationBuilder UseGraphiQL(this IApplicationBuilder app, GraphiQLOptions options) {
            if(app == null) {
                throw new ArgumentNullException(nameof(app));
            }
            if(options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            return app.UseMiddleware<GraphiQLMiddleware>(Options.Create(options));
        }
    }
}
