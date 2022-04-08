using GraphQL.Builders;
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
        /// Gets a boolean value that determines whether any authorization policy is applied to this metadata provider.
        /// </summary>
        /// <param name="provider">
        /// Metadata provider. This can be an instance of <see cref="GraphType"/>,
        /// <see cref="FieldType"/>, <see cref="Schema"/> or others.
        /// </param>
        /// <returns> <c>true</c> if any authorization policy is applied, otherwise <c>false</c>. </returns>
        public static bool RequiresAuthorization(this IProvideMetadata provider) => GetPolicies(provider)?.Count > 0 || GetRoles(provider)?.Count > 0;

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
            where TMetadataProvider : IProvideMetadata
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            var list = GetPolicies(provider) ?? new List<string>();

            if (!list.Contains(policy))
                list.Add(policy);

            provider.Metadata[POLICY_KEY] = list;
            return provider;
        }

        /// <inheritdoc cref="AuthorizeWithPolicy{TMetadataProvider}(TMetadataProvider, string)"/>
        [Obsolete("Please use AuthorizeWithPolicy instead.")]
        public static TMetadataProvider AuthorizeWith<TMetadataProvider>(this TMetadataProvider provider, string policy)
            where TMetadataProvider : IProvideMetadata
            => AuthorizeWithPolicy(provider, policy);

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
            where TMetadataProvider : IProvideMetadata
        {
            if (roles == null)
                throw new ArgumentNullException(nameof(roles));

            var list = GetRoles(provider) ?? new List<string>();

            foreach (var role in roles.Split(','))
            {
                var role2 = role.Trim();
                if (!list.Contains(role2))
                    list.Add(role2);
            }

            provider.Metadata[ROLE_KEY] = list;
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
            where TMetadataProvider : IProvideMetadata
        {
            if (roles == null)
                throw new ArgumentNullException(nameof(roles));

            var list = GetRoles(provider) ?? new List<string>();

            foreach (var role in roles)
            {
                if (!list.Contains(role))
                    list.Add(role);
            }

            provider.Metadata[ROLE_KEY] = list;
            return provider;
        }

        /// <summary>
        /// Adds authorization policy to the specified field builder. If the underlying field already contains
        /// a policy with the same name, then it will not be added twice.
        /// </summary>
        /// <typeparam name="TSourceType"></typeparam>
        /// <typeparam name="TReturnType"></typeparam>
        /// <param name="builder"></param>
        /// <param name="policy"> Authorization policy name. </param>
        /// <returns> The reference to the specified <paramref name="builder"/>. </returns>
        public static FieldBuilder<TSourceType, TReturnType> AuthorizeWithPolicy<TSourceType, TReturnType>(
            this FieldBuilder<TSourceType, TReturnType> builder, string policy)
        {
            builder.FieldType.AuthorizeWithPolicy(policy);
            return builder;
        }

        /// <inheritdoc cref="AuthorizeWith{TSourceType, TReturnType}(FieldBuilder{TSourceType, TReturnType}, string)"/>
        [Obsolete("Please use AuthorizeWithPolicy instead.")]
        public static FieldBuilder<TSourceType, TReturnType> AuthorizeWith<TSourceType, TReturnType>(
            this FieldBuilder<TSourceType, TReturnType> builder, string policy)
            => AuthorizeWithPolicy(builder, policy);

        /// <summary>
        /// Adds authorization role(s) to the specified field builder. Roles should
        /// be comma-separated and role names will be trimmed. If the underlying field already
        /// contains a role with the same name, then it will not be added twice.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="roles"> Comma-separated list of authorization role name(s). </param>
        /// <returns> The reference to the specified <paramref name="builder"/>. </returns>
        public static FieldBuilder<TSourceType, TReturnType> AuthorizeWithRoles<TSourceType, TReturnType>(
            this FieldBuilder<TSourceType, TReturnType> builder, string roles)
        {
            builder.FieldType.AuthorizeWithRoles(roles);
            return builder;
        }

        /// <summary>
        /// Adds authorization role(s) to the specified field builder. If the underlying field already
        /// contains a role with the same name, then it will not be added twice.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="roles"> List of authorization role name(s). </param>
        /// <returns> The reference to the specified <paramref name="builder"/>. </returns>
        public static FieldBuilder<TSourceType, TReturnType> AuthorizeWithRoles<TSourceType, TReturnType>(
            this FieldBuilder<TSourceType, TReturnType> builder, params string[] roles)
        {
            builder.FieldType.AuthorizeWithRoles(roles);
            return builder;
        }

        /// <summary>
        /// Adds authorization policy to the specified connection builder. If the underlying field already
        /// contains a policy with the same name, then it will not be added twice.
        /// </summary>
        /// <typeparam name="TSourceType"></typeparam>
        /// <param name="builder"></param>
        /// <param name="policy"> Authorization policy name. </param>
        /// <returns> The reference to the specified <paramref name="builder"/>. </returns>
        public static ConnectionBuilder<TSourceType> AuthorizeWithPolicy<TSourceType>(
            this ConnectionBuilder<TSourceType> builder, string policy)
        {
            builder.FieldType.AuthorizeWithPolicy(policy);
            return builder;
        }

        /// <inheritdoc cref="AuthorizeWithPolicy{TSourceType}(ConnectionBuilder{TSourceType}, string)"/>
        [Obsolete("Please use AuthorizeWithPolicy instead.")]
        public static ConnectionBuilder<TSourceType> AuthorizeWith<TSourceType>(
            this ConnectionBuilder<TSourceType> builder, string policy)
            => AuthorizeWithPolicy(builder, policy);

        /// <summary>
        /// Adds authorization role(s) to the specified connection builder. Roles should
        /// be comma-separated and role names will be trimmed. If the underlying field already
        /// contains a role with the same name, then it will not be added twice.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="roles"> Comma-separated list of authorization role name(s). </param>
        /// <returns> The reference to the specified <paramref name="builder"/>. </returns>
        public static ConnectionBuilder<TSourceType> AuthorizeWithRoles<TSourceType>(
            this ConnectionBuilder<TSourceType> builder, string roles)
        {
            builder.FieldType.AuthorizeWithRoles(roles);
            return builder;
        }

        /// <summary>
        /// Adds authorization role(s) to the specified connection builder. If the underlying field already
        /// contains a role with the same name, then it will not be added twice.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="roles"> List of authorization role name(s). </param>
        /// <returns> The reference to the specified <paramref name="builder"/>. </returns>
        public static ConnectionBuilder<TSourceType> AuthorizeWithRoles<TSourceType>(
            this ConnectionBuilder<TSourceType> builder, params string[] roles)
        {
            builder.FieldType.AuthorizeWithRoles(roles);
            return builder;
        }
    }
}
