using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation;

public partial class ValidationContext
{
    private Dictionary<GraphQLOperationDefinition, List<GraphQLFragmentDefinition>?>? _fragments = new();
    private Dictionary<GraphQLOperationDefinition, List<GraphQLFragmentDefinition>?>? _usedFragments = new();
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
        var context = new GetRecursivelyReferencedFragmentsVisitorContext(this, onlyUsed);
        GetRecursivelyReferencedFragmentsVisitor.Instance.VisitAsync(operation.SelectionSet, context).GetAwaiter().GetResult();
        var fragments = context.GetFragments(reset: true);
        (onlyUsed ? _usedFragments ??= new() : _fragments ??= new())[operation] = fragments;
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

    // struct intentionally - works only on stack
    private readonly struct GetRecursivelyReferencedFragmentsVisitorContext : IASTVisitorContext
    {
        public GetRecursivelyReferencedFragmentsVisitorContext(ValidationContext validationContext, bool onlyUsedFragments)
        {
            ValidationContext = validationContext;
            OnlyUsedFragments = onlyUsedFragments;
        }

        public bool OnlyUsedFragments { get; }

        public ValidationContext ValidationContext { get; }

        public CancellationToken CancellationToken => default;

        public void AddFragment(GraphQLFragmentDefinition fragment)
            => ValidationContext.AddListItem(nameof(GetRecursivelyReferencedFragmentsVisitorContext), fragment);

        public List<GraphQLFragmentDefinition>? GetFragments(bool reset)
            => ValidationContext.GetList<GraphQLFragmentDefinition>(nameof(GetRecursivelyReferencedFragmentsVisitorContext), reset);
    }

    private sealed class GetRecursivelyReferencedFragmentsVisitor : ASTVisitor<GetRecursivelyReferencedFragmentsVisitorContext>
    {
        private GetRecursivelyReferencedFragmentsVisitor()
        {
        }

        public static GetRecursivelyReferencedFragmentsVisitor Instance { get; } = new();

        public override ValueTask VisitAsync(ASTNode? node, GetRecursivelyReferencedFragmentsVisitorContext context)
        {
            // check if this node should be skipped or not (check @skip and @include directives)
            if (node == null || !context.OnlyUsedFragments || context.ValidationContext.ShouldIncludeNode(node))
                return base.VisitAsync(node, context);

            return default;
        }

        protected override ValueTask VisitOperationDefinitionAsync(GraphQLOperationDefinition operationDefinition, GetRecursivelyReferencedFragmentsVisitorContext context)
            => VisitAsync(operationDefinition.SelectionSet, context);

        protected override ValueTask VisitSelectionSetAsync(GraphQLSelectionSet selectionSet, GetRecursivelyReferencedFragmentsVisitorContext context)
            => VisitAsync(selectionSet.Selections, context);

        protected override ValueTask VisitFieldAsync(GraphQLField field, GetRecursivelyReferencedFragmentsVisitorContext context)
            => VisitAsync(field.SelectionSet, context);

        protected override ValueTask VisitInlineFragmentAsync(GraphQLInlineFragment inlineFragment, GetRecursivelyReferencedFragmentsVisitorContext context)
            => VisitAsync(inlineFragment.SelectionSet, context);

        protected override async ValueTask VisitFragmentSpreadAsync(GraphQLFragmentSpread fragmentSpread, GetRecursivelyReferencedFragmentsVisitorContext context)
        {
            // if we have not encountered this fragment before
            if (!Contains(fragmentSpread, context))
            {
                // find the fragment definition
                var fragmentDefinition = context.ValidationContext.Document.FindFragmentDefinition(fragmentSpread.FragmentName.Name);
                if (fragmentDefinition != null)
                {
                    // add the fragment definition to our known list
                    context.AddFragment(fragmentDefinition);
                    // walk the fragment definition
                    await VisitSelectionSetAsync(fragmentDefinition.SelectionSet, context).ConfigureAwait(false);
                }
            }
        }

        private static bool Contains(GraphQLFragmentSpread fragmentSpread, GetRecursivelyReferencedFragmentsVisitorContext context)
        {
            var fragments = context.GetFragments(reset: false);
            if (fragments == null)
                return false;

            foreach (var frag in fragments)
            {
                if (frag.FragmentName.Name == fragmentSpread.FragmentName.Name)
                    return true;
            }

            return false;
        }
    }
}
