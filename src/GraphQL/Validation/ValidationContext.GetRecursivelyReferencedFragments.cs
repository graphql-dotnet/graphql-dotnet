using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation;

public partial class ValidationContext
{
    private Dictionary<GraphQLOperationDefinition, List<GraphQLFragmentDefinition>?>? _fragments = [];
    private Dictionary<GraphQLOperationDefinition, List<GraphQLFragmentDefinition>?>? _usedFragments = [];
    private bool? _noFragments;

    /// <summary>
    /// For a specified operation within a document, returns a list of all fragment definitions referenced by
    /// the specified operation, or <see langword="null"/> if there are none. When specified by the <paramref name="onlyUsed"/>
    /// parameter, returns only the fragments that are not skipped by the @skip or @include directive.
    /// </summary>
    public List<GraphQLFragmentDefinition>? GetRecursivelyReferencedFragments(GraphQLOperationDefinition operation, bool onlyUsed = false)
    {
        // if we have already determined that there are no fragments in the document, return null
        if (_noFragments == true)
            return null;

        // if we have already determined that there are fragments in the document, check if the fragments have already been calculated
        if (_noFragments == false)
        {
            if ((onlyUsed ? _usedFragments : _fragments)?.TryGetValue(operation, out var results) == true)
            {
                return results;
            }
        }
        else
        {
            // check if there are no fragments in the document (common scenario)
            _noFragments = true;
            foreach (var def in Document.Definitions)
            {
                if (def is GraphQLFragmentDefinition)
                {
                    _noFragments = false;
                    break;
                }
            }

            // no fragments were found
            if (_noFragments == true)
                return null;
        }

        // if we found a fragment, visit the document to find all referenced fragments
        var fragments = CollectReferencedFragments(operation.SelectionSet, onlyUsed);
        (onlyUsed ? _usedFragments ??= [] : _fragments ??= [])[operation] = fragments;
        return fragments;
    }

    // TODO: deduplicate with ExecutionStrategy.ShouldIncludeNode
    /// <inheritdoc cref="Execution.ExecutionStrategy.ShouldIncludeNode{TASTNode}(Execution.ExecutionContext, TASTNode)"/>
    /// <remarks>
    /// Ignores typical parsing, so if BooleanGraphType is overridden (e.g. to coerce strings to booleans), this method
    /// will not reflect the change. Also ignores any overridden behavior within
    /// <see cref="Execution.ExecutionStrategy.ShouldIncludeNode{TASTNode}(Execution.ExecutionContext, TASTNode)">ExecutionStrategy.ShouldIncludeNode</see>.
    /// </remarks>
    public virtual bool ShouldIncludeNode(ASTNode node)
    {
        // according to GraphQL spec, directives with the same name may be defined so long as they cannot be
        // placed on the same node types as other directives with the same name; so here we verify that the
        // node is a field, fragment spread, or inline fragment, the only nodes allowed by the built-in @skip
        // and @include directives
        if (node is not GraphQLField && node is not GraphQLFragmentSpread && node is not GraphQLInlineFragment)
            return true;

        var directives = ((IHasDirectivesNode)node).Directives;
        if (directives == null)
            return true;

        var skipDirective = directives.Find("skip");
        if (skipDirective != null)
        {
            var value = GetDirectiveValue(skipDirective, false);
            if (value)
                return false;
        }

        var includeDirective = directives.Find("include");
        if (includeDirective != null)
        {
            var value = GetDirectiveValue(includeDirective, true);
            if (!value)
                return false;
        }

        return true;

        bool GetDirectiveValue(GraphQLDirective directive, bool defaultValue)
        {
            var ifArg = directive.Arguments?.Find("if");
            if (ifArg != null)
            {
                if (ifArg.Value is GraphQLBooleanValue boolValue)
                {
                    return boolValue.BoolValue;
                }
                else if (ifArg.Value is GraphQLVariable variable && Operation.Variables != null)
                {
                    foreach (var varDef in Operation.Variables.Items)
                    {
                        if (varDef.Variable.Name == variable.Name)
                        {
                            return varDef.Type.Name() == "Boolean"
                                ? (Variables.TryGetValue(variable.Name.StringValue, out var value) && value is bool boolValue2
                                    ? boolValue2
                                    : varDef.DefaultValue is GraphQLBooleanValue boolValue3
                                    ? boolValue3.BoolValue
                                    : defaultValue) // invalid default value or error: variable not specified
                                : defaultValue; // error: variable type is not Boolean
                        }
                    }
                }
            }
            return defaultValue; // error: no "if" argument or invalid argument value
        }
    }

    /// <summary>
    /// For a specified operations within a document, returns a list of all fragment definitions in use, or <see langword="null"/> if there are none.
    /// </summary>
    public List<GraphQLFragmentDefinition>? GetRecursivelyReferencedFragments(List<GraphQLOperationDefinition> operations)
    {
        if (operations.Count == 1)
        {
            return GetRecursivelyReferencedFragments(operations[0]);
        }
        else
        {
            List<GraphQLFragmentDefinition>? fragments = null;
            foreach (var operation in operations)
            {
                var items = GetRecursivelyReferencedFragments(operation);
                if (items != null)
                    (fragments ??= []).AddRange(items);
            }
            return fragments;
        }
    }

    /// <summary>
    /// Uses iterative depth-first traversal to prevent stack overflow exceptions from deeply nested selections or fragment chains.
    /// </summary>
    private List<GraphQLFragmentDefinition>? CollectReferencedFragments(GraphQLSelectionSet selectionSet, bool onlyUsed)
    {
        if (onlyUsed && !ShouldIncludeNode(selectionSet))
            return null;

        // Referenced fragments in discovery order; also used to detect repeated fragments.
        List<GraphQLFragmentDefinition>? fragments = null;
        // Parent selections and the next sibling index to resume.
        using var stack = new StackSlim<(List<ASTNode> selections, int selectionIndex)>();
        var selections = selectionSet.Selections;
        int selectionIndex = 0;

        while (true)
        {
            if (selectionIndex == selections.Count)
            {
                // This selection set is complete; resume the most recent unfinished parent.
                if (!stack.TryPop(out var parent))
                    break;

                (selections, selectionIndex) = parent;
                continue;
            }

            var node = selections[selectionIndex++];
            if (onlyUsed && !ShouldIncludeNode(node))
                continue;

            GraphQLSelectionSet? childSelectionSet;
            if (node is GraphQLFragmentSpread spread)
            {
                if (Contains(fragments, spread.FragmentName.Name))
                    continue;

                var fragment = Document.FindFragmentDefinition(spread.FragmentName.Name);
                if (fragment == null)
                    continue;

                // Mark before descending so cycles and repeated spreads are visited only once.
                (fragments ??= []).Add(fragment);
                childSelectionSet = fragment.SelectionSet;
            }
            else
            {
                childSelectionSet = node switch
                {
                    GraphQLField field => field.SelectionSet,
                    GraphQLInlineFragment inlineFragment => inlineFragment.SelectionSet,
                    _ => null,
                };

                // The inclusion hook applies to field/inline selection sets, but not fragment-definition sets.
                if (childSelectionSet != null && onlyUsed && !ShouldIncludeNode(childSelectionSet))
                    continue;
            }

            if (childSelectionSet == null)
                continue;

            // Save only unfinished siblings, preserving depth-first order. A chain of
            // single selections needs no stack storage, regardless of its depth.
            if (selectionIndex < selections.Count)
                stack.Push((selections, selectionIndex));

            selections = childSelectionSet.Selections;
            selectionIndex = 0;
        }

        return fragments;

        static bool Contains(List<GraphQLFragmentDefinition>? fragments, ROM name)
        {
            if (fragments != null)
            {
                foreach (var fragment in fragments)
                {
                    if (fragment.FragmentName.Name == name)
                        return true;
                }
            }

            return false;
        }
    }
}
