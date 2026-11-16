using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// No unused fragments:
///
/// A GraphQL document is only valid if all fragment definitions are spread
/// within operations, or spread within other fragment spreads within operations.
/// </summary>
public class NoUnusedFragments : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly NoUnusedFragments Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="NoUnusedFragments"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public NoUnusedFragments()
    {
    }

    /// <inheritdoc/>
    /// <exception cref="NoUnusedFragmentsError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
    {
        // If there are no fragment definitions, no need to continue
        foreach (var definition in context.Document.Definitions)
            if (definition is GraphQLFragmentDefinition)
                goto FoundFragment;
        return default;
    FoundFragment:

        // Allocate an operation list only when there is more than one operation.
        GraphQLOperationDefinition? firstOperation = null;
        List<GraphQLOperationDefinition>? operationDefs = null;
        foreach (var definition in context.Document.Definitions)
        {
            if (definition is GraphQLOperationDefinition operation)
            {
                if (firstOperation == null)
                    firstOperation = operation;
                else
                    (operationDefs ??= new(4) { firstOperation }).Add(operation);
            }
        }

        // Get the set of fragments that are used by the collected operations
        var fragmentsUsed = operationDefs != null
            ? context.GetRecursivelyReferencedFragmentsSet(operationDefs)
            : firstOperation != null
                ? context.GetRecursivelyReferencedFragmentsSet(firstOperation)
                : null;

        // Report errors for any fragment definitions that are not used
        foreach (var definition in context.Document.Definitions)
        {
            if (definition is GraphQLFragmentDefinition fragment && (fragmentsUsed == null || !fragmentsUsed.Contains(fragment)))
                context.ReportError(new NoUnusedFragmentsError(context, fragment));
        }

        return default;
    }
}
