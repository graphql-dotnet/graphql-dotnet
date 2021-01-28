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
    /// The default schema filter. By default nothing is hidden. Please note
    /// that some features that are not in the official specification may be
    /// hidden by default. These features can be unlocked using special
    /// <see cref="ExperimentalFeaturesSchemaFilter"/> filter.
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
        public virtual Task<bool> AllowField(IGraphType parent, IFieldType field) => parent.IsIntrospectionType() && field.Name == "appliedDirectives" ? Forbidden : Allowed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowArgument(IFieldType field, QueryArgument argument) => Allowed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowEnumValue(EnumerationGraphType parent, EnumValueDefinition enumValue) => Allowed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowDirective(DirectiveGraphType directive)
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
                    location == DirectiveLocation.InlineFragment))
                    return Forbidden;
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
    public class ExperimentalFeaturesSchemaFilter : DefaultSchemaFilter
    {
        /// <summary>
        /// Experimental features mode.
        /// </summary>
        public ExperimentalIntrospectionFeaturesMode Mode { get; set; } = ExperimentalIntrospectionFeaturesMode.ExecutionOnly;

        /// <inheritdoc/>
        public override Task<bool> AllowType(IGraphType type) => Mode == ExperimentalIntrospectionFeaturesMode.IntrospectionAndExecution ? Allowed : base.AllowType(type);

        /// <inheritdoc/>
        public override Task<bool> AllowField(IGraphType parent, IFieldType field) => Mode == ExperimentalIntrospectionFeaturesMode.IntrospectionAndExecution ? Allowed : base.AllowField(parent, field);
    }

    /// <summary>
    /// A way to use experimental features.
    /// </summary>
    public enum ExperimentalIntrospectionFeaturesMode
    {
        /// <summary>
        /// Allow experimental features only for client queries but not for standard introspection
        /// request. This means that the client, in response to a standard introspection request,
        /// receives a standard response without all the new fields and types. However, client CAN
        /// make requests to the server using the new fields and types. This mode is needed in order
        /// to bypass the problem of tools such as GraphQL Playground, Voyager, GraphiQL that require
        /// a standard response to an introspection request and refuse to work correctly if receive
        /// unknown fields or types in the response.
        /// </summary>
        ExecutionOnly,

        /// <summary>
        /// Allow experimental features for both standard introspection query and client queries.
        /// This means that the client, in response to a standard introspection request, receives
        /// a response augmented with the new fields and types. Client can make requests to the
        /// server using the new fields and types.
        /// </summary>
        IntrospectionAndExecution
    }
}
