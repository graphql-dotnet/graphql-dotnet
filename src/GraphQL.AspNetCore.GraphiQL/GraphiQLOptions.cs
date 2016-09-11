namespace GraphQL.AspNetCore.GraphiQL {
    /// <summary>
    ///     Options for the <see cref="GraphiQLMiddleware" />.
    /// </summary>
    public sealed class GraphiQLOptions {
        public const string DefaultGraphiQLPath = "/graphiql";
        public const string DefaultGraphQLPath = "/graphql";

        /// <summary>
        ///     Create an instance with the default options settings.
        /// </summary>
        public GraphiQLOptions() {
            GraphiQLPath = DefaultGraphiQLPath;
            GraphQLPath = DefaultGraphQLPath;
        }

        /// <summary>
        ///     Provides the path to GraphiQL.
        /// </summary>
        /// <remarks>
        ///     Include leading forward slash.  Defaults to "/graphiql".
        /// </remarks>
        public string GraphiQLPath { get; set; }

        /// <summary>
        ///     Provides the path to GraphQL endpoint.
        /// </summary>
        /// <remarks>
        ///     Include leading forward slash.  Defaults to "/graphql".
        /// </remarks>
        public string GraphQLPath { get; set; }
    }
}
