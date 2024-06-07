using GraphQL.Types;

namespace GraphQL.Federation.Types;

public class LinkPurposeGraphType : EnumerationGraphType<LinkPurpose>
{
    /// <inheritdoc cref="LinkPurposeGraphType"/>
    public LinkPurposeGraphType()
    {
        Name = "link__Purpose";
    }
}
