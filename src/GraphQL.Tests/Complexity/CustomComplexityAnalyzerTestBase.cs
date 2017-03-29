using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Tests.StarWars;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;

namespace GraphQL.Tests.Complexity.CustomComplexityAnalyzer
{
    public class CustomComplexityAnalyzerTestBase
    {
        public IComplexityAnalyzer Analyzer { get; } = new CustomComplexityAnalyzerDefinition();

        public async Task<ExecutionResult> Execute(ExtendedComplexityConfig complexityConfig, string query) =>
            await new DocumentExecuter(new GraphQLDocumentBuilder(), new DocumentValidator(), Analyzer).ExecuteAsync(
                options =>
                {
                    options.Schema = new StarWarsTestBase().Schema;
                    options.Query = query;
                    options.ComplexityConfiguration = complexityConfig;
                });
    }


    public class CustomComplexityAnalyzerDefinition : IComplexityAnalyzer
    {
        private readonly ComplexityResult _result = new ComplexityResult();

        public void Validate(Document document, IComplexityConfiguration parameters)
        {
            if (!(parameters is ExtendedComplexityConfig)) throw new ArgumentException();
            var extendedConfig = (ExtendedComplexityConfig)parameters;

            TreeIterator(document, _result, extendedConfig.complexityMap);
            if (extendedConfig.MaxComplexity.HasValue &&
                _result.Complexity > extendedConfig.MaxComplexity.Value)
                throw new InvalidOperationException("Query is too complex to execute.");

        }

        private void TreeIterator(INode node, ComplexityResult result, Dictionary<string, double> map)
        {
            if (node is Field)
            {
                result.Complexity += map.ContainsKey(((Field)node).Name) ? map[((Field)node).Name] : 1;
                foreach (var nodeChild in node.Children.Where(n => n is SelectionSet))
                    TreeIterator(nodeChild, result, map);
            }

            if (node.Children.Any(n => n is Field || n is SelectionSet && ((SelectionSet)n).Children.Any() || n is Operation))
            {
                foreach (var nodeChild in node.Children)
                    TreeIterator(nodeChild, result, map);
            }
        }
    }
    public class ExtendedComplexityConfig : IComplexityConfiguration
    {
        public double? FieldImpact { get; set; }
        public int? MaxComplexity { get; set; }
        public int? MaxDepth { get; set; }
        public Dictionary<string, double> complexityMap { get; set; }
    }
}