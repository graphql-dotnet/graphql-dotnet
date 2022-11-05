using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation
{
    public partial class ValidationContext
    {
        private static readonly GetRecursivelyReferencedFragmentsVisitor _visitor = new();
        private readonly Dictionary<GraphQLOperationDefinition, List<GraphQLFragmentDefinition>?> _fragments = new();

        /// <summary>
        /// For a specified operation within a document, returns a list of all fragment definitions in use.
        /// </summary>
        public List<GraphQLFragmentDefinition>? GetRecursivelyReferencedFragments(GraphQLOperationDefinition operation)
        {
            if (_fragments.TryGetValue(operation, out var results)) //TODO: possible remove from cache
            {
                return results;
            }

            var context = new GetRecursivelyReferencedFragmentsVisitorContext(this);
            _visitor.VisitAsync(operation.SelectionSet, context).GetAwaiter().GetResult();
            var fragments = context.GetFragments(reset: true);
            _fragments[operation] = fragments;
            return fragments;
        }

        /// <summary>
        /// For a specified operations within a document, returns a list of all fragment definitions in use.
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
                        (fragments ??= new()).AddRange(items);
                }
                return fragments;
            }
        }

        // struct intentionally - works only on stack
        private readonly struct GetRecursivelyReferencedFragmentsVisitorContext : IASTVisitorContext
        {
            public GetRecursivelyReferencedFragmentsVisitorContext(ValidationContext validationContext)
            {
                ValidationContext = validationContext;
            }

            public ValidationContext ValidationContext { get; }

            public CancellationToken CancellationToken => default;

            public void AddFragment(GraphQLFragmentDefinition fragment)
                => ValidationContext.AddListItem(nameof(GetRecursivelyReferencedFragmentsVisitorContext), fragment);

            public List<GraphQLFragmentDefinition>? GetFragments(bool reset)
                => ValidationContext.GetList<GraphQLFragmentDefinition>(nameof(GetRecursivelyReferencedFragmentsVisitorContext), reset);
        }

        private sealed class GetRecursivelyReferencedFragmentsVisitor : ASTVisitor<GetRecursivelyReferencedFragmentsVisitorContext>
        {
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
                if (!Contains(fragmentSpread, context))
                {
                    var fragmentDefinition = context.ValidationContext.Document.FindFragmentDefinition(fragmentSpread.FragmentName.Name);
                    if (fragmentDefinition != null)
                    {
                        context.AddFragment(fragmentDefinition);
                        await VisitSelectionSetAsync(fragmentDefinition.SelectionSet, context).ConfigureAwait(false);
                    }
                }
            }

            private bool Contains(GraphQLFragmentSpread fragmentSpread, GetRecursivelyReferencedFragmentsVisitorContext context)
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
}
