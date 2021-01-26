using System;
using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Extension methods to configure directives applied to GraphQL elements: types, fields, arguments, etc.
    /// </summary>
    public static class DirectivesExtensions
    {
        /// <summary>
        /// Apply directive without specifying arguments. If the directive declaration has arguments,
        /// then their default values (if any) will be used.
        /// </summary>
        /// <param name="provider">
        /// Metadata provider. This can be an instance of <see cref="GraphType"/>,
        /// <see cref="FieldType"/>, <see cref="Schema"/> or others.
        /// </param>
        /// <param name="name">Directive name.</param>
        /// <returns>The reference to the specified <paramref name="provider"/>.</returns>
        public static TMetadataProvider ApplyDirective<TMetadataProvider>(this TMetadataProvider provider, string name)
            where TMetadataProvider : IProvideMetadata => provider.ApplyDirective(name, _ => { });

        /// <summary>
        /// Apply directive specifying one argument. If the directive declaration has other arguments,
        /// then their default values (if any) will be used.
        /// </summary>
        /// <param name="provider">
        /// Metadata provider. This can be an instance of <see cref="GraphType"/>,
        /// <see cref="FieldType"/>, <see cref="Schema"/> or others.
        /// </param>
        /// <param name="name">Directive name.</param>
        /// <param name="argumentName">Argument name.</param>
        /// <param name="argumentValue">Argument value.</param>
        /// <returns>The reference to the specified <paramref name="provider"/>.</returns>
        public static TMetadataProvider ApplyDirective<TMetadataProvider>(this TMetadataProvider provider, string name, string argumentName, object argumentValue)
            where TMetadataProvider : IProvideMetadata => provider.ApplyDirective(name, directive => directive.AddArgument(new DirectiveArgument(argumentName) { Value = argumentValue }));

        /// <summary>
        /// Apply directive with configuration delegate.
        /// </summary>
        /// <param name="provider">
        /// Metadata provider. This can be an instance of <see cref="GraphType"/>,
        /// <see cref="FieldType"/>, <see cref="Schema"/> or others.
        /// </param>
        /// <param name="name">Directive name.</param>
        /// <param name="configure">Configuration delegate.</param>
        /// <returns>The reference to the specified <paramref name="provider"/>.</returns>
        public static TMetadataProvider ApplyDirective<TMetadataProvider>(this TMetadataProvider provider, string name, Action<AppliedDirective> configure)
            where TMetadataProvider : IProvideMetadata
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var directive = new AppliedDirective(name);
            configure(directive);
            provider.AppliedDirectives.Add(directive);

            return provider;
        }
    }
}
