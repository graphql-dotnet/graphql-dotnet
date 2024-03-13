using GraphQL.Utilities.Federation;

namespace GraphQL.Federation.Types;

/// <summary>
/// The purpose of the link.
/// </summary>
internal class LinkPurposeGraphType : AnyScalarGraphType
{
    /// <inheritdoc cref="LinkPurposeGraphType"/>
    public LinkPurposeGraphType()
    {
        Name = "link__Purpose";
    }
}
