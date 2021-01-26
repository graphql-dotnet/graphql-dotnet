using System.Threading.Tasks;
using GraphQL.Types;

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
    /// Please note that some features may be hidden by default
    /// that are not in the official specification. These features
    /// can be unlocked using your own filter.
    /// </summary>
    public class DefaultSchemaFilter : ISchemaFilter
    {
        private static readonly Task<bool> _allowed = Task.FromResult(true);
        private static readonly Task<bool> _forbidden = Task.FromResult(false);

        /// <inheritdoc/>
        public virtual Task<bool> AllowType(IGraphType type) => type is __AppliedDirective || type is __DirectiveArgument ? _forbidden : _allowed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowField(IGraphType parent, IFieldType field) => parent is __Field && field.Name == "directives" ? _forbidden : _allowed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowArgument(IFieldType field, QueryArgument argument) => _allowed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowEnumValue(EnumerationGraphType parent, EnumValueDefinition enumValue) => _allowed;

        public virtual Task<bool> AllowDirective(DirectiveGraphType directive)
        {
            if (directive.Introspectable.HasValue)
                return directive.Introspectable.Value ? _allowed : _forbidden;

            // If the directive has all its locations of type ExecutableDirectiveLocation,
            // only then it will be present in the introspection response.
            foreach (var location in directive.Locations)
            {
                if (!(
                    location == DirectiveLocation.Query ||
                    location == DirectiveLocation.Mutation ||
                    location == DirectiveLocation.Subscription ||
                    location == DirectiveLocation.Field ||
                    location == DirectiveLocation.FragmentDefinition ||
                    location == DirectiveLocation.FragmentSpread ||
                    location == DirectiveLocation.InlineFragment))
                    return _forbidden;
            }

            return _allowed;
        }
    }
}
