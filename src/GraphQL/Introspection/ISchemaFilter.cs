using GraphQL.Types;
using GraphQLParser.AST;

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
        Task<bool> AllowDirective(Directive directive);
    }

    /// <summary>
    /// The default schema filter. By default nothing is hidden. Please note
    /// that some features that are not in the official specification may be
    /// hidden by default. These features can be unlocked using special
    /// <see cref="ExperimentalIntrospectionFeaturesSchemaFilter"/> filter.
    /// </summary>
    public class DefaultSchemaFilter : ISchemaFilter
    {
        /// <summary>
        /// Cached <c>Task.FromResult(true)</c>.
        /// </summary>
        protected static readonly Task<bool> Allowed = Task.FromResult(true);

        /// <summary>
        /// Cached <c>Task.FromResult(false)</c>.
        /// </summary>
        protected static readonly Task<bool> Forbidden = Task.FromResult(false);

        /// <inheritdoc/>
        public virtual Task<bool> AllowType(IGraphType type) => type is __AppliedDirective || type is __DirectiveArgument ? Forbidden : Allowed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowField(IGraphType parent, IFieldType field) => parent.IsIntrospectionType() && (field.Name == "appliedDirectives" || field.Name == "isRepeatable") ? Forbidden : Allowed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowArgument(IFieldType field, QueryArgument argument) => Allowed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowEnumValue(EnumerationGraphType parent, EnumValueDefinition enumValue) => Allowed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowDirective(Directive directive)
        {
            if (directive.Introspectable.HasValue)
                return directive.Introspectable.Value ? Allowed : Forbidden;

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
                    location == DirectiveLocation.InlineFragment ||
                    location == DirectiveLocation.VariableDefinition))
                {
                    return Forbidden;
                }
            }

            return Allowed;
        }
    }

    /// <summary>
    /// Schema filter that enables some experimental features that are not in the
    /// official specification, i.e. ability to expose user-defined meta-information
    /// via introspection. See https://github.com/graphql/graphql-spec/issues/300
    /// for more information.
    /// </summary>
    public class ExperimentalIntrospectionFeaturesSchemaFilter : DefaultSchemaFilter
    {
        /// <inheritdoc/>
        public override Task<bool> AllowType(IGraphType type) => Allowed;

        /// <inheritdoc/>
        public override Task<bool> AllowField(IGraphType parent, IFieldType field) => Allowed;
    }
}
