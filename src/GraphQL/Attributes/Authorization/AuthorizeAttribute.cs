using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL;

/// <summary>
/// Attribute to apply an authorization policy and/or roles to a graph, field or query argument.
/// Marks the graph, field or query argument as requiring authentication even if no policies or
/// roles are specified. This attribute mimics AuthorizeAttribute from ASP.NET Core so it is
/// something people are likely used to, if they do any web programming in C#.
/// </summary>
public class AuthorizeAttribute : GraphQLAttribute
{
    /// <summary>
    /// Creates an empty instance of <see cref="AuthorizeAttribute"/> with no policy/roles specified.
    /// </summary>
    public AuthorizeAttribute()
    {
    }

    /// <summary>
    /// Creates an instance with the specified policy name.
    /// </summary>
    public AuthorizeAttribute(string policy)
    {
        Policy = policy;
    }

    /// <summary>
    /// The name of policy to apply.
    /// </summary>
    public string? Policy { get; set; }

    /// <summary>
    /// A comma-separated list of the roles to apply.
    /// Role names will be trimmed before adding.
    /// </summary>
    public string? Roles { get; set; }

    /// <inheritdoc />
    public override void Modify(TypeConfig type)
    {
        type.Authorize();

        if (Policy != null)
            type.AuthorizeWithPolicy(Policy);

        if (Roles != null)
            type.AuthorizeWithRoles(Roles);
    }

    /// <inheritdoc />
    public override void Modify(FieldConfig field)
    {
        field.Authorize();

        if (Policy != null)
            field.AuthorizeWithPolicy(Policy);

        if (Roles != null)
            field.AuthorizeWithRoles(Roles);
    }

    /// <inheritdoc />
    public override void Modify(IGraphType graphType)
    {
        graphType.Authorize();

        if (Policy != null)
            graphType.AuthorizeWithPolicy(Policy);

        if (Roles != null)
            graphType.AuthorizeWithRoles(Roles);
    }

    /// <inheritdoc />
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        fieldType.Authorize();

        if (Policy != null)
            fieldType.AuthorizeWithPolicy(Policy);

        if (Roles != null)
            fieldType.AuthorizeWithRoles(Roles);
    }

    /// <inheritdoc />
    public override void Modify(QueryArgument queryArgument)
    {
        queryArgument.Authorize();

        if (Policy != null)
            queryArgument.AuthorizeWithPolicy(Policy);

        if (Roles != null)
            queryArgument.AuthorizeWithRoles(Roles);
    }

    /// <inheritdoc />
    public override void Modify(EnumValueDefinition enumValueDefinition)
    {
        enumValueDefinition.Authorize();

        if (Policy != null)
            enumValueDefinition.AuthorizeWithPolicy(Policy);

        if (Roles != null)
            enumValueDefinition.AuthorizeWithRoles(Roles);
    }
}
