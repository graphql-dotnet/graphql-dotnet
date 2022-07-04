using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation.Complexity
{
    /// <summary>
    /// Two-phase complexity visitor. See <see cref="ComplexityAnalyzer.Analyze(GraphQLDocument, double, int)"/>.
    /// Phase 1. Calculate complexity of all fragments defined in GraphQL document; <see cref="AnalysisContext.FragmentMapAlreadyBuilt"/> is false.
    /// Phase 2. Calculate complexity of executed operation; <see cref="AnalysisContext.FragmentMapAlreadyBuilt"/> is true.
    /// </summary>
    internal class ComplexityVisitor : ASTVisitor<AnalysisContext>
    {
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

            if (context.FragmentMapAlreadyBuilt)
            {
                if (field.SelectionSet == null) // leaf field
                {
                    context.RecordFieldComplexity(field, context.CurrentEndNodeImpact);
                }
                else
                {
                    context.Result.TotalQueryDepth++;

                    double? impactFromArgs = AnalysisContext.GetImpactFromArgs(field);
                    context.CurrentEndNodeImpact = impactFromArgs == null
                        ? context.CurrentSubSelectionImpact
                        : impactFromArgs.Value / context.AvgImpact * context.CurrentSubSelectionImpact;

                    context.RecordFieldComplexity(field, context.CurrentEndNodeImpact);
                    context.CurrentSubSelectionImpact *= impactFromArgs ?? context.AvgImpact;
                }
            }
            else
            {
                if (field.SelectionSet == null) // leaf field
                {
                    context.CurrentFragmentComplexity.Complexity += context.CurrentEndNodeImpact;
                }
                else
                {
                    context.CurrentFragmentComplexity.Depth++;

                    double? impactFromArgs = AnalysisContext.GetImpactFromArgs(field);
                    context.CurrentEndNodeImpact = impactFromArgs == null
                        ? context.CurrentSubSelectionImpact
                        : impactFromArgs.Value / context.AvgImpact * context.CurrentSubSelectionImpact;

                    context.CurrentFragmentComplexity.Complexity += context.CurrentEndNodeImpact;
                    context.CurrentSubSelectionImpact *= impactFromArgs ?? context.AvgImpact;
                }
            }

            await base.VisitFieldAsync(field, context).ConfigureAwait(false);

            context.CurrentSubSelectionImpact = prevCurrentSubSelectionImpact;
            context.CurrentEndNodeImpact = prevCurrentEndNodeImpact;
        }

        protected override async ValueTask VisitFragmentSpreadAsync(GraphQLFragmentSpread fragmentSpread, AnalysisContext context)
        {
            var fragmentComplexity = context.FragmentMap[fragmentSpread.FragmentName.Name.StringValue];

            context.RecordFieldComplexity(fragmentSpread, context.CurrentSubSelectionImpact / context.AvgImpact * fragmentComplexity.Complexity);
            context.Result.TotalQueryDepth += fragmentComplexity.Depth;

            await base.VisitFragmentSpreadAsync(fragmentSpread, context).ConfigureAwait(false);
        }
    }
}
