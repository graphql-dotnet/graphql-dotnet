using System.Diagnostics;
using GraphQLParser.AST;

namespace GraphQL.Validation.Complexity
{
    /// <summary>
    /// The default complexity analyzer.
    /// </summary>
    public class ComplexityAnalyzer : IComplexityAnalyzer
    {
        /// <inheritdoc/>
        public void Validate(GraphQLDocument document, ComplexityConfiguration complexityParameters)
        {
            if (complexityParameters == null)
                return;
            var complexityResult = Analyze(document, complexityParameters.FieldImpact ?? 2.0f, complexityParameters.MaxRecursionCount);

            Analyzed(document, complexityParameters, complexityResult);

            if (complexityResult.Complexity > complexityParameters.MaxComplexity)
                throw new InvalidOperationException(
                    $"Query is too complex to execute. The field with the highest complexity is: {complexityResult.ComplexityMap.OrderByDescending(pair => pair.Value).First().Key}");

            if (complexityResult.TotalQueryDepth > complexityParameters.MaxDepth)
                throw new InvalidOperationException(
                    $"Query is too nested to execute. Depth is {complexityResult.TotalQueryDepth} levels, maximum allowed on this endpoint is {complexityParameters.MaxDepth}.");
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
        internal ComplexityResult Analyze(GraphQLDocument doc, double avgImpact, int maxRecursionCount)
        {
            if (avgImpact <= 1)
                throw new ArgumentOutOfRangeException(nameof(avgImpact));

            var context = new AnalysisContext
            {
                MaxRecursionCount = maxRecursionCount,
                AvgImpact = avgImpact,
                CurrentSubSelectionImpact = avgImpact,
                CurrentEndNodeImpact = 1d
            };
            var visitor = new ComplexityVisitor();

            // https://github.com/graphql-dotnet/graphql-dotnet/issues/3030
            foreach (var frag in doc.Definitions.OfType<GraphQLFragmentDefinition>().OrderBy(x => x, new NestedFragmentsComparer(doc)))
                visitor.VisitAsync(frag, context).GetAwaiter().GetResult();

            context.FragmentMapAlreadyBuilt = true;

            visitor.VisitAsync(doc, context).GetAwaiter().GetResult();

            return context.Result;
        }
    }
}
