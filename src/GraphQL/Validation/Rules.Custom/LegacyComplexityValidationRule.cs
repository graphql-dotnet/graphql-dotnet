using System.Diagnostics;
using GraphQL.Types;
using GraphQL.Validation.Complexity;
using GraphQL.Validation.Errors.Custom;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules.Custom;

/// <summary>
/// Analyzes a document to determine if its complexity exceeds a threshold.
/// </summary>
[Obsolete("Please use the new complexity analyzer. The v7 complexity analyzer will be removed in v9.")]
public class LegacyComplexityValidationRule : ValidationRuleBase, INodeVisitor
{
    private LegacyComplexityConfiguration ComplexityConfiguration { get; }

    /// <summary>
    /// Initializes an instance with the specified complexity configuration.
    /// </summary>
    public LegacyComplexityValidationRule(LegacyComplexityConfiguration complexityConfiguration)
    {
        ComplexityConfiguration = complexityConfiguration;
    }

    /// <inheritdoc/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(this);

    ValueTask INodeVisitor.EnterAsync(ASTNode node, ValidationContext context) => default;

    ValueTask INodeVisitor.LeaveAsync(ASTNode node, ValidationContext context)
    {
        // Complexity analysis should run at the very end.
        if (node is GraphQLDocument)
        {
            // Fast return here to avoid all possible problems with complexity analysis.
            // For example, document may contain fragment cycles, see https://github.com/graphql-dotnet/graphql-dotnet/issues/3527
            // Note, that ComplexityValidationRule/ComplexityAnalyzer still checks for fragment cycles since there may be no standard rules configured.
            if (context.HasErrors)
                return default;

            try
            {
                using (context.Metrics.Subject("document", "Analyzing complexity"))
                    Validate(context.Document, context.Schema);
            }
            catch (ComplexityError ex)
            {
                context.ReportError(ex);
            }
        }

        return default;
    }

    /// <inheritdoc/>
    public void Validate(GraphQLDocument document, ISchema schema)
    {
        var complexityResult = Analyze(document, ComplexityConfiguration.FieldImpact ?? 2.0f, ComplexityConfiguration.MaxRecursionCount, schema);

        Analyzed(document, complexityResult);

        if (complexityResult.Complexity > ComplexityConfiguration.MaxComplexity)
            throw new ComplexityError(
                $"Query is too complex to execute. Complexity is {complexityResult.Complexity}, maximum allowed on this endpoint is {ComplexityConfiguration.MaxComplexity}. The field with the highest complexity is '{GetName(complexityResult.ComplexityMap.OrderByDescending(pair => pair.Value).First().Key)}' with value {complexityResult.ComplexityMap.OrderByDescending(pair => pair.Value).First().Value}.");

        if (complexityResult.TotalQueryDepth > ComplexityConfiguration.MaxDepth)
            throw new ComplexityError(
                $"Query is too nested to execute. Depth is {complexityResult.TotalQueryDepth} levels, maximum allowed on this endpoint is {ComplexityConfiguration.MaxDepth}.");

        string GetName(ASTNode node)
        {
            return node switch
            {
                GraphQLField f => f.Name.StringValue,
                GraphQLFragmentSpread fs => fs.FragmentName.Name.StringValue,
                _ => throw new NotSupportedException(node.ToString()),
            };
        }
    }

    /// <summary>
    /// Executes after the complexity analysis has completed, before comparing results to the complexity configuration parameters.
    /// This method is made to be able to access the calculated <see cref="LegacyComplexityResult"/> and handle it, for example, for logging.
    /// </summary>
    protected virtual void Analyzed(GraphQLDocument document, LegacyComplexityResult complexityResult)
    {
#if DEBUG
        Debug.WriteLine($"Complexity: {complexityResult.Complexity}");
        Debug.WriteLine($"Sum(Query depth across all subqueries) : {complexityResult.TotalQueryDepth}");
        foreach (var node in complexityResult.ComplexityMap)
            Debug.WriteLine($"{node.Key} : {node.Value}");
#endif
    }

    /// <summary>
    /// Analyzes the complexity of a document.
    /// </summary>
    internal static LegacyComplexityResult Analyze(GraphQLDocument doc, double avgImpact, int maxRecursionCount, ISchema? schema = null)
    {
        if (avgImpact <= 1)
            throw new ArgumentOutOfRangeException(nameof(avgImpact));

        var context = new LegacyAnalysisContext
        {
            MaxRecursionCount = maxRecursionCount,
            AvgImpact = avgImpact,
            CurrentEndNodeImpact = 1d
        };
        var visitor = schema == null ? new LegacyComplexityVisitor() : new LegacyComplexityVisitor(schema);

        // https://github.com/graphql-dotnet/graphql-dotnet/issues/3030
        // Sort fragment definitions so that independent fragments go in front.
        var dependencies = BuildDependencies(doc);
        List<GraphQLFragmentDefinition> orderedFragments = [];
        List<GraphQLFragmentDefinition> fragsToNull = [];

        while (dependencies.Count > 0)
        {
            var independentFragment = GetFirstFragmentWithoutPendingDependencies(dependencies);

            orderedFragments.Add(independentFragment);
            dependencies.Remove(independentFragment);

            foreach (var item in dependencies) // no deconstruct syntax for netstandard2.0
            {
                if (item.Value?.Remove(independentFragment) == true && item.Value.Count == 0)
                    fragsToNull.Add(item.Key);
            }

            // next candidates for GetFirstFragmentWithoutPendingDependencies
            foreach (var frag in fragsToNull)
                dependencies[frag] = null;

            fragsToNull.Clear();
        }

        foreach (var frag in orderedFragments)
            visitor.VisitAsync(frag, context).GetAwaiter().GetResult();

        context.FragmentMapAlreadyBuilt = true;

        visitor.VisitAsync(doc, context).GetAwaiter().GetResult();

        return context.Result;
    }

    private static GraphQLFragmentDefinition GetFirstFragmentWithoutPendingDependencies(Dictionary<GraphQLFragmentDefinition, HashSet<GraphQLFragmentDefinition>?> dependencies)
    {
        foreach (var item in dependencies)
        {
            if (item.Value == null)
                return item.Key;
        }

        throw new InvalidOperationException("Fragments dependency cycle detected!");
    }

    private static Dictionary<GraphQLFragmentDefinition, HashSet<GraphQLFragmentDefinition>?> BuildDependencies(GraphQLDocument document)
    {
        Stack<GraphQLSelectionSet> _selectionSetsToVisit = new();
        Dictionary<GraphQLFragmentDefinition, HashSet<GraphQLFragmentDefinition>?> dependencies = [];

        foreach (var fragmentDef in document.Definitions.OfType<GraphQLFragmentDefinition>())
        {
            dependencies[fragmentDef] = GetDependencies(fragmentDef);
        }

        return dependencies;

        HashSet<GraphQLFragmentDefinition>? GetDependencies(GraphQLFragmentDefinition def)
        {
            HashSet<GraphQLFragmentDefinition>? dependencies = null;
            _selectionSetsToVisit.Push(def.SelectionSet);

            int counter = 0;
            const int MAX_ITERATIONS = 2000;
            while (_selectionSetsToVisit.Count > 0)
            {
                // https://github.com/graphql-dotnet/graphql-dotnet/issues/3527
                if (++counter > MAX_ITERATIONS)
                    throw new ValidationError("It looks like document has fragment cycle. Please make sure you are using standard validation rules especially NoFragmentCycles one.");

                foreach (var selection in _selectionSetsToVisit.Pop().Selections)
                {
                    if (selection is GraphQLFragmentSpread spread)
                    {
                        var frag = document.FindFragmentDefinition(spread.FragmentName.Name.Value);
                        if (frag != null)
                        {
                            (dependencies ??= []).Add(frag);
                            _selectionSetsToVisit.Push(frag.SelectionSet);
                        }
                    }
                    else if (selection is IHasSelectionSetNode hasSet && hasSet.SelectionSet != null)
                    {
                        _selectionSetsToVisit.Push(hasSet.SelectionSet);
                    }
                }
            }

            _selectionSetsToVisit.Clear();
            return dependencies;
        }
    }
}
