using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation
{
    public partial class ValidationContext
    {
        private static readonly GetVariablesVisitor _getVariablesVisitor = new();
        private readonly Dictionary<GraphQLOperationDefinition, List<VariableUsage>?> _variables = new();

        /// <summary>
        /// For a node with a selection set, returns a list of variable references along with what input type each were referenced for.
        /// </summary>
        public List<VariableUsage>? GetVariables<TNode>(TNode node)
            where TNode : ASTNode, IHasSelectionSetNode
        {
            var context = new GetVariablesVisitorContext(this, new TypeInfo(Schema));
            _getVariablesVisitor.VisitAsync(node, context).GetAwaiter().GetResult();
            return context.GetVariableUsages();
        }

        /// <summary>
        /// For a specified operation with a document, returns a list of variable references
        /// along with what input type each was referenced for.
        /// </summary>
        public List<VariableUsage>? GetRecursiveVariables(GraphQLOperationDefinition operation)
        {
            if (_variables.TryGetValue(operation, out var results))
            {
                return results;
            }

            var usages = GetVariables(operation);

            var frags = GetRecursivelyReferencedFragments(operation);

            if (frags != null)
            {
                foreach (var fragment in frags)
                {
                    var usagesFromFragment = GetVariables(fragment);
                    if (usagesFromFragment != null)
                    {
                        if (usages == null)
                            usages = usagesFromFragment;
                        else
                            usages.AddRange(usagesFromFragment);
                    }
                }
            }

            _variables[operation] = usages;

            return usages;
        }

        // struct intentionally - works only on stack
        private readonly struct GetVariablesVisitorContext : IASTVisitorContext
        {
            public GetVariablesVisitorContext(ValidationContext validationContext, TypeInfo typeInfo)
            {
                ValidationContext = validationContext;
                Info = typeInfo;
            }

            public ValidationContext ValidationContext { get; }

            public TypeInfo Info { get; }

            public CancellationToken CancellationToken => default;

            public void AddVariableUsage(VariableUsage usage)
                => ValidationContext.AddListItem(nameof(GetVariablesVisitorContext), usage);

            public List<VariableUsage>? GetVariableUsages()
                => ValidationContext.GetList<VariableUsage>(nameof(GetVariablesVisitorContext), reset: true);
        }

        private sealed class GetVariablesVisitor : ASTVisitor<GetVariablesVisitorContext>
        {
            public override async ValueTask VisitAsync(ASTNode? node, GetVariablesVisitorContext context)
            {
                if (node == null)
                    return;

                await context.Info.EnterAsync(node, context.ValidationContext).ConfigureAwait(false);

                await base.VisitAsync(node, context).ConfigureAwait(false);

                await context.Info.LeaveAsync(node, context.ValidationContext).ConfigureAwait(false);
            }

            protected override ValueTask VisitVariableAsync(GraphQLVariable variable, GetVariablesVisitorContext context)
            {
                // GraphQLVariable AST node represents both variable definition and variable usage so check parent node
                if (context.Info.GetAncestor(1) is not GraphQLVariableDefinition)
                    context.AddVariableUsage(new VariableUsage(variable, context.Info.GetInputType()!));
                return default;
            }
        }
    }
}
