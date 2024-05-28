using GraphQL.Types;

namespace GraphQL.Federation.Types;

/// <summary>
/// The purpose of the link.
/// </summary>
internal class LinkPurposeGraphType : EnumerationGraphType<LinkPurpose>
{
    /// <inheritdoc cref="LinkPurposeGraphType"/>
    public LinkPurposeGraphType()
    {
        Name = "link__Purpose";
    }
}
