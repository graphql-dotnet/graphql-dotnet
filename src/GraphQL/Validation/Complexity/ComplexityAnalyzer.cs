using System.Diagnostics;
using GraphQL.Types;
using GraphQL.Validation.Errors.Custom;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Complexity
{
    /// <summary>
    /// The default complexity analyzer.
    /// </summary>
    [Obsolete("Please write a custom complexity analyzer as a validation rule. This class will be removed in v8.")]
    public class ComplexityAnalyzer : IComplexityAnalyzer
    {
        /// <inheritdoc/>
        public void Validate(GraphQLDocument document, ComplexityConfiguration complexityParameters, ISchema? schema = null)
        {
            if (complexityParameters == null)
                return;
            var complexityResult = Analyze(document, complexityParameters.FieldImpact ?? 2.0f, complexityParameters.MaxRecursionCount, schema);

            Analyzed(document, complexityParameters, complexityResult);

            if (complexityResult.Complexity > complexityParameters.MaxComplexity)
                throw new ComplexityError(
                    $"Query is too complex to execute. Complexity is {complexityResult.Complexity}, maximum allowed on this endpoint is {complexityParameters.MaxComplexity}. The field with the highest complexity is '{GetName(complexityResult.ComplexityMap.OrderByDescending(pair => pair.Value).First().Key)}' with value {complexityResult.ComplexityMap.OrderByDescending(pair => pair.Value).First().Value}.");

            if (complexityResult.TotalQueryDepth > complexityParameters.MaxDepth)
                throw new ComplexityError(
                    $"Query is too nested to execute. Depth is {complexityResult.TotalQueryDepth} levels, maximum allowed on this endpoint is {complexityParameters.MaxDepth}.");

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
        /// This method is made to be able to access the calculated <see cref="ComplexityResult"/> and handle it, for example, for logging.
        /// </summary>
        protected virtual void Analyzed(GraphQLDocument document, ComplexityConfiguration complexityParameters, ComplexityResult complexityResult)
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
        internal ComplexityResult Analyze(GraphQLDocument doc, double avgImpact, int maxRecursionCount, ISchema? schema = null)
        {
            if (avgImpact <= 1)
                throw new ArgumentOutOfRangeException(nameof(avgImpact));

            var context = new AnalysisContext
            {
                MaxRecursionCount = maxRecursionCount,
                AvgImpact = avgImpact,
                CurrentEndNodeImpact = 1d
            };
            var visitor = schema == null ? new ComplexityVisitor() : new ComplexityVisitor(schema);

            // https://github.com/graphql-dotnet/graphql-dotnet/issues/3030
            // Sort fragment definitions so that independent fragments go in front.
            var dependencies = BuildDependencies(doc);
            List<GraphQLFragmentDefinition> orderedFragments = new();
            List<GraphQLFragmentDefinition> fragsToNull = new();

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
            Dictionary<GraphQLFragmentDefinition, HashSet<GraphQLFragmentDefinition>?> dependencies = new();

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
                                (dependencies ??= new()).Add(frag);
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
}
