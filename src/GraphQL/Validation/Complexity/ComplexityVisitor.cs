using System.Threading.Tasks;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation.Complexity
{
    internal class ComplexityVisitor : DefaultNodeVisitor<AnalysisContext>
    {
        public override async ValueTask VisitFragmentDefinition(GraphQLFragmentDefinition fragmentDefinition, AnalysisContext context)
        {
            if (!context.FragmentMapAlreadyBuilt)
            {
                context.FragmentMap[fragmentDefinition.FragmentName.Name.StringValue] = context.CurrentFragmentComplexity = new FragmentComplexity();

                await base.VisitFragmentDefinition(fragmentDefinition, context).ConfigureAwait(false);

                context.CurrentFragmentComplexity = null!;
            }
        }

        public override async ValueTask VisitField(GraphQLField field, AnalysisContext context)
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

            await base.VisitField(field, context);

            context.CurrentSubSelectionImpact = prevCurrentSubSelectionImpact;
            context.CurrentEndNodeImpact = prevCurrentEndNodeImpact;
        }

        public override async ValueTask VisitFragmentSpread(GraphQLFragmentSpread fragmentSpread, AnalysisContext context)
        {
            var fragmentComplexity = context.FragmentMap[fragmentSpread.FragmentName.Name.StringValue];

            context.RecordFieldComplexity(fragmentSpread, context.CurrentSubSelectionImpact / context.AvgImpact * fragmentComplexity.Complexity);
            context.Result.TotalQueryDepth += fragmentComplexity.Depth;

            await base.VisitFragmentSpread(fragmentSpread, context);
        }
    }
}
