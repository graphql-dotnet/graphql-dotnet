using GraphQL.Types;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.Introspection
{
    /// <summary>
    /// Provides the ability to filter the schema upon introspection to hide types, fields, arguments, enum values, and directives.
    /// Can be used to hide information, such as graph types, from clients that have an inadequate permission level.
    /// </summary>
    public interface ISchemaFilter
    {
        /// <summary>
        /// Returns a boolean indicating whether the specified graph type should be returned within the introspection query.
        /// </summary>
        /// <param name="type">The graph type to consider.</param>
        Task<bool> AllowType(IGraphType type);
        /// <summary>
        /// Returns a boolean indicating whether the specified field should be returned within the introspection query.
        /// </summary>
        /// <param name="parent">The parent type to which the field belongs.</param>
        /// <param name="field">The field to consider.</param>
        Task<bool> AllowField(IGraphType parent, IFieldType field);
        /// <summary>
        /// Returns a boolean indicating whether the specified argument should be returned within the introspection query.
        /// </summary>
        /// <param name="field">The field to which the argument belongs.</param>
        /// <param name="argument">The argument to consider.</param>
        Task<bool> AllowArgument(IFieldType field, QueryArgument argument);
        /// <summary>
        /// Returns a boolean indicating whether the specified enumeration value should be returned within the introspection query.
        /// </summary>
        /// <param name="parent">The enumeration to which the enumeration value belongs.</param>
        /// <param name="enumValue">The enumeration value to consider.</param>
        Task<bool> AllowEnumValue(EnumerationGraphType parent, EnumValueDefinition enumValue);
        /// <summary>
        /// Returns a boolean indicating whether the specified directive should be returned within the introspection query.
        /// </summary>
        /// <param name="directive">The directive to consider.</param>
        Task<bool> AllowDirective(DirectiveGraphType directive);
    }

    /// <summary>
    /// The default schema filter. By default nothing is hidden.
    /// </summary>
    public class DefaultSchemaFilter : ISchemaFilter
    {
        private static readonly Task<bool> _completed = Task.FromResult(true);

        /// <inheritdoc/>
        public virtual Task<bool> AllowType(IGraphType type) => _completed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowField(IGraphType parent, IFieldType field) => _completed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowArgument(IFieldType field, QueryArgument argument) => _completed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowEnumValue(EnumerationGraphType parent, EnumValueDefinition enumValue) => _completed;

        public virtual Task<bool> AllowDirective(DirectiveGraphType directive)
        {
            if (directive.Introspectable.HasValue)
                return Task.FromResult(directive.Introspectable.Value);

            // true for all ExecutableDirectiveLocation
            return Task.FromResult(directive.Locations.All(l =>
                l == DirectiveLocation.Query ||
                l == DirectiveLocation.Mutation ||
                l == DirectiveLocation.Subscription ||
                l == DirectiveLocation.Field ||
                l == DirectiveLocation.FragmentDefinition ||
                l == DirectiveLocation.FragmentSpread ||
                l == DirectiveLocation.InlineFragment));
        }
    }
}
