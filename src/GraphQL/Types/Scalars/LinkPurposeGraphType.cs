using GraphQL.Types;

namespace GraphQL.Types.Scalars;

/// <summary>
/// Represents an enumeration type for link purposes in GraphQL Federation.
/// Used to define the purpose of a link directive, such as "SECURITY" or "EXECUTION".
/// The name of this graph type is "link__Purpose".
/// </summary>
public class LinkPurposeGraphType : EnumerationGraphType<LinkPurpose>
{
    /// <inheritdoc cref="LinkPurposeGraphType"/>
    public LinkPurposeGraphType()
    {
        Name = "link__Purpose";
    }
}
