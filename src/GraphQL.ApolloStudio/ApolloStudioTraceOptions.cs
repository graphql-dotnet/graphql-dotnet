using System;

namespace GraphQL.ApolloStudio
{
    /// <summary>
    /// Options for configuration Apollo Studio trace logging
    /// </summary>
    public class ApolloStudioTraceOptions
    {
        private const string DEFAULT_REPORTING_URI = "https://engine-report.apollodata.com/api/ingress/traces";
        private const string DEFAULT_CLIENT_NAME_HEADER = "apollographql-client-name";
        private const string DEFAULT_CLIENT_VERSION_HEADER = "apollographql-client-version";

        /// <summary>
        /// Apollo Studio API Key
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Schema tag (usually indicates environment)
        /// </summary>
        public string SchemaTag { get; set; }

        /// <summary>
        /// URI to send data to (defaults to "https://engine-report.apollodata.com/api/ingress/traces")
        /// </summary>
        public Uri ReportingUri { get; set; } = new Uri(DEFAULT_REPORTING_URI);

        /// <summary>
        /// HTTP header to extract the client name from (defaults to "apollographql-client-name")
        /// </summary>
        public string ClientNameHeader { get; set; } = DEFAULT_CLIENT_NAME_HEADER;

        /// <summary>
        /// HTTP header to extract the client version from (defaults to "apollographql-client-version")
        /// </summary>
        public string ClientVersionHeader { get; set; } = DEFAULT_CLIENT_VERSION_HEADER;
    }
}
