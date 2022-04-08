using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL
{
    /// <summary>
    /// Attribute to apply an authorization policy and/or roles to a graph or field.
    /// </summary>
    public class GraphQLAuthorizeAttribute : GraphQLAttribute
    {
        /// <summary>
        /// Creates an empty instance of <see cref="GraphQLAuthorizeAttribute"/> with no policy/roles specified.
        /// </summary>
        public GraphQLAuthorizeAttribute()
        {
        }

        /// <summary>
        /// Creates an instance with the specified policy name.
        /// </summary>
        public GraphQLAuthorizeAttribute(string policy)
        {
            Policy = policy;
        }

        /// <summary>
        /// The name of policy to apply.
        /// </summary>
        public string? Policy { get; init; }

        /// <summary>
        /// A comma-separated list of the roles to apply.
        /// Role names will be trimmed before adding.
        /// </summary>
        public string? Roles { get; init; }

        /// <inheritdoc />
        public override void Modify(TypeConfig type)
        {
            if (Policy != null)
                type.AuthorizeWithPolicy(Policy);

            if (Roles != null)
                type.AuthorizeWithRoles(Roles);
        }

        /// <inheritdoc />
        public override void Modify(FieldConfig field)
        {
            if (Policy != null)
                field.AuthorizeWithPolicy(Policy);

            if (Roles != null)
                field.AuthorizeWithRoles(Roles);
        }

        /// <inheritdoc />
        public override void Modify(IGraphType graphType)
        {
            if (Policy != null)
                graphType.AuthorizeWithPolicy(Policy);

            if (Roles != null)
                graphType.AuthorizeWithRoles(Roles);
        }

        /// <inheritdoc />
        public override void Modify(FieldType fieldType, bool isInputType)
        {
            if (Policy != null)
                fieldType.AuthorizeWithPolicy(Policy);

            if (Roles != null)
                fieldType.AuthorizeWithRoles(Roles);
        }
    }
}
