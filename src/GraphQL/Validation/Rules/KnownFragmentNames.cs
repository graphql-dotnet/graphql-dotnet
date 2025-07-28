using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// Known fragment names:
///
/// A GraphQL document is only valid if all <c>...Fragment</c> fragment spreads refer
/// to fragments defined in the same document.
/// </summary>
public sealed class KnownFragmentNames : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
    public static readonly KnownFragmentNames Instance = new();
    private KnownFragmentNames() { }

    /// <inheritdoc/>
    /// <exception cref="KnownFragmentNamesError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_nodeVisitor);

    private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLFragmentSpread>((node, context) =>
    {
        var fragmentName = node.FragmentName.Name;
        var fragment = context.Document.FindFragmentDefinition(fragmentName);
        if (fragment == null)
        {
            context.ReportError(new KnownFragmentNamesError(context, node, fragmentName.StringValue));
        }
    });
}
