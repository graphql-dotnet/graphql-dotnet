using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// Represents an filter for information returned from introspection queries.
    /// Can be used to hide information, such as graph types, from clients that have an inadequate permission level.
    /// </summary>
    public interface ISchemaFilter
    {
        /// <summary>
        /// Returns a boolean indicating whether the specified graph type should be returned within the introspection query.
        /// </summary>
        Task<bool> AllowType(IGraphType type);
        /// <summary>
        /// Returns a boolean indicating whether the specified field should be returned within the introspection query.
        /// </summary>
        Task<bool> AllowField(IGraphType parent, IFieldType field);
        /// <summary>
        /// Returns a boolean indicating whether the specified argument should be returned within the introspection query.
        /// </summary>
        Task<bool> AllowArgument(IFieldType field, QueryArgument argument);
        /// <summary>
        /// Returns a boolean indicating whether the specified enumeration value should be returned within the introspection query.
        /// </summary>
        Task<bool> AllowEnumValue(EnumerationGraphType parent, EnumValueDefinition enumValue);
        /// <summary>
        /// Returns a boolean indicating whether the specified directive should be returned within the introspection query.
        /// </summary>
        Task<bool> AllowDirective(DirectiveGraphType directive);
    }

    /// <summary>
    /// A schema filter which allows all requested information to introspection queries.
    /// </summary>
    public class DefaultSchemaFilter : ISchemaFilter
    {
        /// <inheritdoc/>
        public virtual Task<bool> AllowType(IGraphType type) => Task.FromResult(true);

        /// <inheritdoc/>
        public virtual Task<bool> AllowField(IGraphType parent, IFieldType field) => Task.FromResult(true);

        /// <inheritdoc/>
        public virtual Task<bool> AllowArgument(IFieldType field, QueryArgument argument) => Task.FromResult(true);

        /// <inheritdoc/>
        public virtual Task<bool> AllowEnumValue(EnumerationGraphType parent, EnumValueDefinition enumValue) => Task.FromResult(true);

        /// <inheritdoc/>
        public virtual Task<bool> AllowDirective(DirectiveGraphType directive) => Task.FromResult(true);
    }
}
