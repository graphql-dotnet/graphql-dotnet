using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// Unique fragment names:
///
/// A GraphQL document is only valid if all defined fragments have unique names.
/// </summary>
public class UniqueFragmentNames : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly UniqueFragmentNames Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="UniqueFragmentNames"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public UniqueFragmentNames()
    {
    }

    /// <inheritdoc/>
    /// <exception cref="UniqueFragmentNamesError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(context.Document.FragmentsCount() > 1 ? _nodeVisitor : null);

    private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLFragmentDefinition>((fragmentDefinition, context) =>
        {
            var knownFragments = context.TypeInfo.UniqueFragmentNames_KnownFragments ??= new();

            var fragmentName = fragmentDefinition.FragmentName.Name;
            if (knownFragments.TryGetValue(fragmentName, out var frag)) // .NET 2.2+ has TryAdd
            {
                context.ReportError(new UniqueFragmentNamesError(context, frag, fragmentDefinition));
            }
            else
            {
                knownFragments[fragmentName] = fragmentDefinition;
            }
        });
}
