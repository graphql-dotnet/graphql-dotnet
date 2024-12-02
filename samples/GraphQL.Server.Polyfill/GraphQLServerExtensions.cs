using GraphQL.Server.Polyfill;
using GraphQL.Types;

namespace Microsoft.AspNetCore.Builder;

public static class GraphQLServerExtensions
{
    /// <summary>
    /// Add the GraphQL middleware to the HTTP request pipeline for the specified schema.
    /// </summary>
    /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
    /// <param name="builder">The application builder</param>
    /// <param name="path">The path to the GraphQL endpoint</param>
    /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
    public static IApplicationBuilder UseGraphQL<TSchema>(this IApplicationBuilder builder, string path = "/graphql")
        where TSchema : ISchema
    {
        return builder.UseWhen(
            context => context.Request.Path.Equals(path),
            b => b.UseMiddleware<GraphQLHttpMiddleware<TSchema>>([]));
    }

    /// <inheritdoc cref="UseGraphQL{TSchema}(IApplicationBuilder, string)"/>
    public static IApplicationBuilder UseGraphQL(this IApplicationBuilder builder, string path = "/graphql")
        => UseGraphQL<ISchema>(builder, path);
}
