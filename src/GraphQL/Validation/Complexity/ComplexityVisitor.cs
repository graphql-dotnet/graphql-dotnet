using GraphQL.Types;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation.Complexity
{
    /// <summary>
    /// Two-phase complexity visitor. See <see cref="ComplexityAnalyzer.Analyze(GraphQLDocument, double, int, ISchema?)"/>.
    /// Phase 1. Calculate complexity of all fragments defined in GraphQL document; <see cref="AnalysisContext.FragmentMapAlreadyBuilt"/> is false.
    /// Phase 2. Calculate complexity of executed operation; <see cref="AnalysisContext.FragmentMapAlreadyBuilt"/> is true.
    /// </summary>
    internal class ComplexityVisitor : ASTVisitor<AnalysisContext>
    {
        private readonly TypeInfo? _visitor;

        public ComplexityVisitor()
        {
        }

        public ComplexityVisitor(ISchema schema)
        {
            _visitor = new TypeInfo(schema);
        }

        public override async ValueTask VisitAsync(ASTNode? node, AnalysisContext context)
        {
            if (node != null)
            {
                if (_visitor != null)
                    await _visitor.EnterAsync(node, null!).ConfigureAwait(false);

                await base.VisitAsync(node, context).ConfigureAwait(false);

                if (_visitor != null)
                    await _visitor.LeaveAsync(node, null!).ConfigureAwait(false);
            }
        }

        protected override async ValueTask VisitFragmentDefinitionAsync(GraphQLFragmentDefinition fragmentDefinition, AnalysisContext context)
        {
            if (!context.FragmentMapAlreadyBuilt)
            {
                context.FragmentMap[fragmentDefinition.FragmentName.Name.StringValue] = context.CurrentFragmentComplexity = new FragmentComplexity();

                await base.VisitFragmentDefinitionAsync(fragmentDefinition, context).ConfigureAwait(false);

                context.CurrentFragmentComplexity = null!;
            }
        }

        protected override async ValueTask VisitFieldAsync(GraphQLField field, AnalysisContext context)
        {
            context.AssertRecursion();

            var prevCurrentSubSelectionImpact = context.CurrentSubSelectionImpact;
            var prevCurrentEndNodeImpact = context.CurrentEndNodeImpact;

            var fieldImpact = _visitor?.GetFieldDef()?.GetComplexityImpact() ?? context.AvgImpact;
            var zeroImpact = fieldImpact == 0;
            context.CurrentSubSelectionImpact ??= (zeroImpact ? context.AvgImpact : fieldImpact);

            if (context.FragmentMapAlreadyBuilt)
            {
                if (field.SelectionSet == null) // leaf field
                {
                    context.RecordFieldComplexity(field, zeroImpact ? 0 : context.CurrentEndNodeImpact);
                }
                else
                {
                    context.Result.TotalQueryDepth++;

                    double? impactFromArgs = AnalysisContext.GetImpactFromArgs(field);
                    context.CurrentEndNodeImpact = impactFromArgs == null
                        ? context.CurrentSubSelectionImpact.Value
                        : impactFromArgs.Value / context.AvgImpact * context.CurrentSubSelectionImpact.Value;

                    context.RecordFieldComplexity(field, zeroImpact ? 0 : context.CurrentEndNodeImpact);
                    context.CurrentSubSelectionImpact *= impactFromArgs ?? (zeroImpact ? context.AvgImpact : fieldImpact);
                }
            }
            else
            {
                if (field.SelectionSet == null) // leaf field
                {
                    context.CurrentFragmentComplexity.Complexity += zeroImpact ? 0 : context.CurrentEndNodeImpact;
                }
                else
                {
                    context.CurrentFragmentComplexity.Depth++;

                    double? impactFromArgs = AnalysisContext.GetImpactFromArgs(field);
                    context.CurrentEndNodeImpact = impactFromArgs == null
                        ? context.CurrentSubSelectionImpact.Value
                        : impactFromArgs.Value / context.AvgImpact * context.CurrentSubSelectionImpact.Value;

                    context.CurrentFragmentComplexity.Complexity += zeroImpact ? 0 : context.CurrentEndNodeImpact;
                    context.CurrentSubSelectionImpact *= impactFromArgs ?? (zeroImpact ? context.AvgImpact : fieldImpact);
                }
            }

            await base.VisitFieldAsync(field, context).ConfigureAwait(false);

            context.CurrentSubSelectionImpact = prevCurrentSubSelectionImpact;
            context.CurrentEndNodeImpact = prevCurrentEndNodeImpact;
        }

        protected override async ValueTask VisitFragmentSpreadAsync(GraphQLFragmentSpread fragmentSpread, AnalysisContext context)
        {
            var fragmentComplexity = context.FragmentMap[fragmentSpread.FragmentName.Name.StringValue];

            var complexity = (context.CurrentSubSelectionImpact ?? context.AvgImpact) / context.AvgImpact * fragmentComplexity.Complexity;
            if (context.FragmentMapAlreadyBuilt)
            {
                context.RecordFieldComplexity(fragmentSpread, complexity);
                context.Result.TotalQueryDepth += fragmentComplexity.Depth;
            }
            else
            {
                context.CurrentFragmentComplexity.Complexity += complexity;
                context.CurrentFragmentComplexity.Depth += fragmentComplexity.Depth;
            }

            await base.VisitFragmentSpreadAsync(fragmentSpread, context).ConfigureAwait(false);
        }
    }
}
