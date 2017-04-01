using GraphQL.Validation.PreciseComplexity;

namespace GraphQL.Tests.PreciseComplexity
{
    using GraphQL.Execution;
    using GraphQL.Language.AST;
    using System.Linq;

    public abstract class PreciseComplexityTestBase
    {
        protected PreciseComplexityAnalyser.Result Analyze(string query, string variables = null)
        {
            var configuration = new PreciseComplexityConfiguration { DefaultCollectionChildrenCount = 10 };
            var schema = new PreciseComplexitySchema();
            schema.Initialize();
            var documentBuilder = new GraphQLDocumentBuilder();
            var document = documentBuilder.Build(query);

            var analyzer = new PreciseComplexityAnalyser();

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

            return analyzer.Analyze(
                executer,
                context,
                executer.GetOperationRootType(document, schema, operation),
                operation.SelectionSet);
        }
    }
}
