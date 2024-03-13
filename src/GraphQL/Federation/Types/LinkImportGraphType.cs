using GraphQL.Utilities.Federation;

namespace GraphQL.Federation.Types;

/// <summary>
/// Specifies the directive to import and optionally its name.
/// </summary>
internal class LinkImportGraphType : AnyScalarGraphType
{
    /// <inheritdoc cref="LinkImportGraphType"/>
    public LinkImportGraphType()
    {
        Name = "link__Import";
    }
}
