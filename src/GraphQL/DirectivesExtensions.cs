using System;
using GraphQL.Introspection;
using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Extension methods to configure directives applied to GraphQL elements: types, fields, arguments, etc.
    /// </summary>
    public static class DirectivesExtensions
    {
        private const string DIRECTIVES_KEY = "__APPLIED__DIRECTIVES__";

        /// <summary>
        /// Indicates whether provider has any applied directives.
        /// </summary>
        public static bool HasAppliedDirectives(this IProvideMetadata provider) => provider.HasMetadata(DIRECTIVES_KEY);

        /// <summary>
        /// Provides all directives applied to this provider if any.
        /// Otherwise returns <see langword="null"/>.
        /// </summary>
        public static AppliedDirectives GetAppliedDirectives(this IProvideMetadata provider) => provider.GetMetadata<AppliedDirectives>(DIRECTIVES_KEY);

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

            var directives = provider.GetAppliedDirectives() ?? new AppliedDirectives();
            directives.Add(directive);

            provider.Metadata[DIRECTIVES_KEY] = directives;

            return provider;
        }

        internal static void AddAppliedDirectivesField<TSourceType>(this ComplexGraphType<TSourceType> type, string element)
            where TSourceType : IProvideMetadata
        {
            type.FieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<__AppliedDirective>>>>(
               name: "appliedDirectives",
               description: $"Directives applied to the {element}",
               resolve: async context =>
               {
                   if (context.Source.HasAppliedDirectives())
                   {
                       var appliedDirectives = context.Source.GetAppliedDirectives();
                       var result = context.ArrayPool.Rent<AppliedDirective>(appliedDirectives.Count);

                       int index = 0;
                       foreach (var applied in appliedDirectives.List)
                       {
                           // return only registered directives allowed by filter
                           var schemaDirective = context.Schema.Directives.Find(applied.Name);
                           if (schemaDirective != null && await context.Schema.Filter.AllowDirective(schemaDirective))
                           {
                               result[index++] = applied;
                           }
                       }

                       return result.Constrained(index);
                   }

                   return Array.Empty<AppliedDirective>();
               });
        }
    }
}
