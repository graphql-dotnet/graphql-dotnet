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
public class KnownFragmentNames : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly KnownFragmentNames Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="KnownFragmentNames"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public KnownFragmentNames()
    {
    }

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
