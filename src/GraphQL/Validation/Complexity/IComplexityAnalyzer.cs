using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Validation.Complexity
{
    /// <summary>
    /// Analyzes a document to determine if its complexity exceeds a threshold,
    /// throwing an exception if it is too complex.
    /// </summary>
    [Obsolete("Please write a custom complexity analyzer as a validation rule. This interface will be removed in v8.")]
    public interface IComplexityAnalyzer
    {
        /// <summary>
        /// Analyzes a document to determine if its complexity exceeds a threshold,
        /// throwing an exception if it is too complex.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="parameters"></param>
        /// <param name="schema"></param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if complexity is not within the defined range in parameters.
        /// </exception>
        void Validate(GraphQLDocument document, ComplexityConfiguration parameters, ISchema? schema = null);
    }
}
