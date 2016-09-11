using GraphQL.Types;

namespace GraphQL.AspNetCore.GraphQL {
    /// <summary>
    ///     Options for the <see cref="GraphQLMiddleware" />.
    /// </summary>
    public sealed class GraphQLOptions {
        /// <summary>
        ///     The default GraphQL path.
        /// </summary>
        public const string DefaultGraphQLPath = "/graphql";

        /// <summary>
        ///     Create an instance with the default options settings.
        /// </summary>
        public GraphQLOptions() {
            GraphQLPath = DefaultGraphQLPath;
        }

        /// <summary>
        ///     Provides the path to GraphQL endpoint.
        /// </summary>
        /// <remarks>
        ///     Include leading forward slash.  Defaults to "/graphql".
        /// </remarks>
        public string GraphQLPath { get; set; }

        /// <summary>
        ///     Provides the schema instance.
        /// </summary>
        public ISchema Schema { get; set; }
    }
}
