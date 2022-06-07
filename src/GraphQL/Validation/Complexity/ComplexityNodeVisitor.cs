using GraphQLParser.AST;

namespace GraphQL.Validation.Complexity;

internal class ComplexityNodeVisitor : AnalysisContext, INodeVisitor
{
    private readonly Stack<(double?, double)> _fieldInfo = new();

    public ValueTask EnterAsync(ASTNode node, ValidationContext context)
    {
        if (node is GraphQLFragmentDefinition fragmentDefinition)
        {
            FragmentMap[fragmentDefinition.FragmentName.Name.StringValue] = CurrentFragmentComplexity = new FragmentComplexity();
        }
        else if (node is GraphQLField field)
        {
            AssertRecursion();

            var prevCurrentSubSelectionImpact = CurrentSubSelectionImpact;
            var prevCurrentEndNodeImpact = CurrentEndNodeImpact;

            var fieldImpact = context.TypeInfo.GetFieldDef()?.GetComplexityImpact() ?? AvgImpact;
            var zeroImpact = fieldImpact == 0;
            CurrentSubSelectionImpact ??= (zeroImpact ? AvgImpact : fieldImpact);

            if (FragmentMapAlreadyBuilt)
            {
                if (field.SelectionSet == null) // leaf field
                {
                    RecordFieldComplexity(field, zeroImpact ? 0 : CurrentEndNodeImpact);
                }
                else
                {
                    Result.TotalQueryDepth++;

                    double? impactFromArgs = AnalysisContext.GetImpactFromArgs(field);
                    CurrentEndNodeImpact = impactFromArgs == null
                        ? CurrentSubSelectionImpact.Value
                        : impactFromArgs.Value / AvgImpact * CurrentSubSelectionImpact.Value;

                    RecordFieldComplexity(field, zeroImpact ? 0 : CurrentEndNodeImpact);
                    CurrentSubSelectionImpact *= impactFromArgs ?? (zeroImpact ? AvgImpact : fieldImpact);
                }
            }
            else
            {
                if (field.SelectionSet == null) // leaf field
                {
                    CurrentFragmentComplexity.Complexity += zeroImpact ? 0 : CurrentEndNodeImpact;
                }
                else
                {
                    CurrentFragmentComplexity.Depth++;

                    double? impactFromArgs = AnalysisContext.GetImpactFromArgs(field);
                    CurrentEndNodeImpact = impactFromArgs == null
                        ? CurrentSubSelectionImpact.Value
                        : impactFromArgs.Value / AvgImpact * CurrentSubSelectionImpact.Value;

                    CurrentFragmentComplexity.Complexity += zeroImpact ? 0 : CurrentEndNodeImpact;
                    CurrentSubSelectionImpact *= impactFromArgs ?? (zeroImpact ? AvgImpact : fieldImpact);
                }
            }

            _fieldInfo.Push((prevCurrentSubSelectionImpact, prevCurrentEndNodeImpact));
        }
        else if (node is GraphQLFragmentSpread fragmentSpread)
        {
            var fragmentComplexity = FragmentMap[fragmentSpread.FragmentName.Name.StringValue];

            var complexity = (CurrentSubSelectionImpact ?? AvgImpact) / AvgImpact * fragmentComplexity.Complexity;
            if (FragmentMapAlreadyBuilt)
            {
                RecordFieldComplexity(fragmentSpread, complexity);
                Result.TotalQueryDepth += fragmentComplexity.Depth;
            }
            else
            {
                CurrentFragmentComplexity.Complexity += complexity;
                CurrentFragmentComplexity.Depth += fragmentComplexity.Depth;
            }
        }

        return default;
    }

    public ValueTask LeaveAsync(ASTNode node, ValidationContext context)
    {
        if (node is GraphQLFragmentDefinition)
        {
            CurrentFragmentComplexity = null!;
        }
        else if (node is GraphQLField field)
        {
            var (prevCurrentSubSelectionImpact, prevCurrentEndNodeImpact) = _fieldInfo.Pop();
            CurrentSubSelectionImpact = prevCurrentSubSelectionImpact;
            CurrentEndNodeImpact = prevCurrentEndNodeImpact;
        }

        return default;
    }
}
