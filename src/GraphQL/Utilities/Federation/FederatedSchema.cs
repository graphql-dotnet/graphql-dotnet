using GraphQL.Types;

namespace GraphQL.Utilities.Federation
{
    /// <summary>
    /// A schema builder for GraphQL federation
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1102:Make class static.", Justification = "TODO: rewrite")]
    public class FederatedSchema
    {
        /// <summary>
        /// Builds schema from the specified string and configuration delegate.
        /// </summary>
        /// <param name="typeDefinitions">A textual description of the schema in SDL (Schema Definition Language) format.</param>
        /// <param name="configure">Optional configuration delegate to setup <see cref="SchemaBuilder"/>.</param>
        /// <returns>Created schema.</returns>
        public static Schema For(string typeDefinitions, Action<FederatedSchemaBuilder>? configure = null)
            => For<FederatedSchemaBuilder>(typeDefinitions, configure);

        /// <summary>
        /// Builds schema from the specified string and configuration delegate.
        /// </summary>
        /// <typeparam name="TFederatedSchemaBuilder">The type of <see cref="FederatedSchemaBuilder"/> that will create the schema.</typeparam>
        /// <param name="typeDefinitions">A textual description of the schema in SDL (Schema Definition Language) format.</param>
        /// <param name="configure">Optional configuration delegate to setup <see cref="SchemaBuilder"/>.</param>
        /// <returns>Created schema.</returns>
        public static Schema For<TFederatedSchemaBuilder>(string typeDefinitions, Action<TFederatedSchemaBuilder>? configure = null)
            where TFederatedSchemaBuilder : FederatedSchemaBuilder, new()
        {
            var builder = new TFederatedSchemaBuilder();
            configure?.Invoke(builder);
            return builder.Build(typeDefinitions);
        }
    }
}
