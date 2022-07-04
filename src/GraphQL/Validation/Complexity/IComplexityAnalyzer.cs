using GraphQLParser.AST;

namespace GraphQL.Validation.Complexity
{
    /// <summary>
    /// Analyzes a document to determine if its complexity exceeds a threshold,
    /// throwing an exception if it is too complex.
    /// </summary>
    public interface IComplexityAnalyzer
    {
        /// <summary>
        /// Analyzes a document to determine if its complexity exceeds a threshold,
        /// throwing an exception if it is too complex.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="parameters"></param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if complexity is not within the defined range in parameters.
        /// </exception>
        void Validate(GraphQLDocument document, ComplexityConfiguration parameters);
    }
}
