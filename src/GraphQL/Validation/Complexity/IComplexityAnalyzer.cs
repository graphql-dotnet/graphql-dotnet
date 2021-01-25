using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Complexity
{
    /// <summary>
    /// Analyzes a document to determine if its complexity exceeds a threshold, throwing an exception if it is too complex.
    /// </summary>
    public interface IComplexityAnalyzer
    {
        /// <exception cref="InvalidOperationException">
        /// Thrown if complexity is not within the defined range in parameters.
        /// </exception>
        void Validate(Document document, ComplexityConfiguration parameters);
    }
}
