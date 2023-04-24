using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Extension methods to configure authorization requirements for GraphQL elements: types, fields, schema.
    /// </summary>
    public static class AuthorizationExtensions
    {
        /// <summary>
        /// Metadata key name for storing authorization policy names. Value of this key
        /// is a simple list of strings.
        /// </summary>
        public const string POLICY_KEY = "Authorization__Policies";

        /// <summary>
        /// Metadata key name for storing authorization role names. Value of this key
        /// is a simple list of strings.
        /// </summary>
        public const string ROLE_KEY = "Authorization__Roles";

        /// <summary>
        /// Metadata key name for indicating that the user must be authorized (strictly speaking, authenticated) to access the resource.
        /// Value of this key is a boolean value.
        /// </summary>
        public const string AUTHORIZE_KEY = "Authorization__Required";

        /// <summary>
        /// Metadata key name for typically indicating if anonymous access should be allowed to a field of a graph type
        /// requiring authorization, providing that no other fields were selected.
        /// </summary>
        public const string ANONYMOUS_KEY = "Authorization__AllowAnonymous";

        /// <summary>
        /// Gets a list of authorization policy names for the specified metadata provider if any.
        /// Otherwise returns <see langword="null"/>.
        /// </summary>
        /// <param name="provider">
        /// Metadata provider. This can be an instance of <see cref="GraphType"/>,
        /// <see cref="FieldType"/>, <see cref="Schema"/> or others.
        /// </param>
        /// <returns> List of authorization policy names applied to this metadata provider. </returns>
        public static List<string>? GetPolicies(this IProvideMetadata provider) => provider.GetMetadata<List<string>>(POLICY_KEY);

        /// <summary>
        /// Gets a list of authorization roles names for the specified metadata provider if any.
        /// Otherwise returns <see langword="null"/>.
        /// </summary>
        /// <param name="provider">
        /// Metadata provider. This can be an instance of <see cref="GraphType"/>,
        /// <see cref="FieldType"/>, <see cref="Schema"/> or others.
        /// </param>
        /// <returns> List of authorization role names applied to this metadata provider. </returns>
        public static List<string>? GetRoles(this IProvideMetadata provider) => provider.GetMetadata<List<string>>(ROLE_KEY);

        /// <summary>
        /// Returns a boolean typically indicating if anonymous access should be allowed to a field of a graph type
        /// requiring authorization, providing that no other fields were selected.
        /// </summary>
        public static bool IsAnonymousAllowed(this IProvideMetadata provider) => provider.GetMetadata(ANONYMOUS_KEY, false);

        /// <summary>
        /// Adds metadata to typically indicate that anonymous access should be allowed to a field of a graph type
        /// requiring authorization, providing that no other fields were selected.
        /// </summary>
        public static TMetadataProvider AllowAnonymous<TMetadataProvider>(this TMetadataProvider provider)
            where TMetadataProvider : IMetadataBuilder
        {
            provider.Metadata[ANONYMOUS_KEY] = true;
            return provider;
        }

        /// <summary>
        /// Gets a boolean value that determines whether any authorization policy is applied to this metadata provider.
        /// </summary>
        /// <param name="provider">
        /// Metadata provider. This can be an instance of <see cref="GraphType"/>,
        /// <see cref="FieldType"/>, <see cref="Schema"/> or others.
        /// </param>
        /// <returns> <see langword="true"/> if any authorization policy is applied, otherwise <see langword="false"/>. </returns>
        public static bool IsAuthorizationRequired(this IProvideMetadata provider)
            => provider.GetMetadata(AUTHORIZE_KEY, false) || GetPolicies(provider)?.Count > 0 || GetRoles(provider)?.Count > 0;

        /// <summary>
        /// Adds metadata to indicate that the resource requires that the user has successfully authenticated.
        /// </summary>
        /// <param name="provider">
        /// Metadata provider. This can be an instance of <see cref="GraphType"/>,
        /// <see cref="FieldType"/>, <see cref="Schema"/> or others.
        /// </param>
        public static TMetadataProvider Authorize<TMetadataProvider>(this TMetadataProvider provider)
            where TMetadataProvider : IMetadataBuilder
        {
            provider.Metadata[AUTHORIZE_KEY] = true;
            return provider;
        }

        /// <summary>
        /// Adds authorization policy to the specified metadata provider. If the provider already contains
        /// a policy with the same name, then it will not be added twice.
        /// </summary>
        /// <typeparam name="TMetadataProvider"> The type of metadata provider. Generics are used here to
        /// let compiler infer the returning type to allow methods chaining.
        /// </typeparam>
        /// <param name="provider">
        /// Metadata provider. This can be an instance of <see cref="GraphType"/>,
        /// <see cref="FieldType"/>, <see cref="Schema"/> or others.
        /// </param>
        /// <param name="policy"> Authorization policy name. </param>
        /// <returns> The reference to the specified <paramref name="provider"/>. </returns>
        public static TMetadataProvider AuthorizeWithPolicy<TMetadataProvider>(this TMetadataProvider provider, string policy)
            where TMetadataProvider : IMetadataBuilder
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            var list = provider.GetMetadata<List<string>>(POLICY_KEY) ?? new List<string>();

            if (!list.Contains(policy))
                list.Add(policy);

            provider.Metadata[POLICY_KEY] = list;
            provider.Authorize();
            return provider;
        }

        /// <summary>
        /// Adds authorization role(s) to the specified metadata provider. Roles should
        /// be comma-separated and role names will be trimmed. If the underlying field already
        /// contains a role with the same name, then it will not be added twice.
        /// </summary>
        /// <typeparam name="TMetadataProvider"> The type of metadata provider. Generics are used here to
        /// let compiler infer the returning type to allow methods chaining.
        /// </typeparam>
        /// <param name="provider">
        /// Metadata provider. This can be an instance of <see cref="GraphType"/>,
        /// <see cref="FieldType"/>, <see cref="Schema"/> or others.
        /// </param>
        /// <param name="roles"> Comma-separated list of authorization role name(s). </param>
        /// <returns> The reference to the specified <paramref name="provider"/>. </returns>
        public static TMetadataProvider AuthorizeWithRoles<TMetadataProvider>(this TMetadataProvider provider, string roles)
            where TMetadataProvider : IMetadataBuilder
        {
            if (roles == null)
                throw new ArgumentNullException(nameof(roles));

            var list = provider.GetMetadata<List<string>>(ROLE_KEY) ?? new List<string>();

            foreach (var role in roles.Split(','))
            {
                var role2 = role.Trim();
                if (role2 != "" && !list.Contains(role2))
                    list.Add(role2);
            }

            provider.Metadata[ROLE_KEY] = list;
            provider.Authorize();
            return provider;
        }

        /// <summary>
        /// Adds authorization role(s) to the specified metadata provider.  If the underlying field already
        /// contains a role with the same name, then it will not be added twice.
        /// </summary>
        /// <typeparam name="TMetadataProvider"> The type of metadata provider. Generics are used here to
        /// let compiler infer the returning type to allow methods chaining.
        /// </typeparam>
        /// <param name="provider">
        /// Metadata provider. This can be an instance of <see cref="GraphType"/>,
        /// <see cref="FieldType"/>, <see cref="Schema"/> or others.
        /// </param>
        /// <param name="roles"> List of authorization role name(s). </param>
        /// <returns> The reference to the specified <paramref name="provider"/>. </returns>
        public static TMetadataProvider AuthorizeWithRoles<TMetadataProvider>(this TMetadataProvider provider, params string[] roles)
            where TMetadataProvider : IMetadataBuilder
        {
            if (roles == null)
                throw new ArgumentNullException(nameof(roles));

            var list = provider.GetMetadata<List<string>>(ROLE_KEY) ?? new List<string>();

            foreach (var role in roles)
            {
                if (role != "" && !list.Contains(role))
                    list.Add(role);
            }

            provider.Metadata[ROLE_KEY] = list;
            provider.Authorize();
            return provider;
        }
    }
}
