using GraphQL.Validation.PreciseComplexity;
using GraphQL.Execution;
using System.Linq;

namespace GraphQL.Tests.PreciseComplexity
{
    public abstract class PreciseComplexityTestBase
    {
        protected PreciseComplexityAnalyser.Result Analyze(
            string query,
            string variables = null,
            int defaultCollectionChildrenCount = 10,
            double? maxComplexity = null,
            int? maxDepth = null)
        {
            var configuration = new PreciseComplexityConfiguration
                                    {
                                        DefaultCollectionChildrenCount =
                                            defaultCollectionChildrenCount,
                                        MaxComplexity = maxComplexity,
                                        MaxDepth = maxDepth
            };
            var schema = new PreciseComplexitySchema();
            schema.Initialize();
            var documentBuilder = new GraphQLDocumentBuilder();
            var document = documentBuilder.Build(query);

            var executer = new DocumentExecuter();

            var context = new PreciseComplexityContext
                              {
                                  Configuration = configuration,
                                  Schema = schema,
                                  Document = document,
                                  Fragments = document.Fragments,
                              };

            var operation = document.Operations.FirstOrDefault();

            if (variables != null)
            {
                context.Variables = executer.GetVariableValues(
                    document,
                    schema,
                    operation.Variables,
                    variables.ToInputs());
            }

            var analyzer = new PreciseComplexityAnalyser(executer, context);
            return analyzer.Analyze(
                executer.GetOperationRootType(document, schema, operation),
                operation.SelectionSet);
        }
    }
}
