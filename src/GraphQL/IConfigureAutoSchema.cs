using GraphQL.DI;

namespace GraphQL
{
    /// <summary>
    /// Provides configuration for an auto-schema.
    /// See <see cref="GraphQLBuilderExtensions.AddAutoSchema{TQueryClrType}(IGraphQLBuilder, Action{IConfigureAutoSchema})"/>.
    /// </summary>
    public interface IConfigureAutoSchema
    {
        /// <summary>
        /// Returns a <see cref="IGraphQLBuilder"/> reference that can be used to configure the schema or service provider.
        /// </summary>
        IGraphQLBuilder Builder { get; }
    }

    internal class ConfigureAutoSchema : IConfigureAutoSchema
    {
        public ConfigureAutoSchema(IGraphQLBuilder baseBuilder)
        {
            Builder = baseBuilder;
        }

        public IGraphQLBuilder Builder { get; }
    }
}
