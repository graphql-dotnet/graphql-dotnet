using System.Collections.Generic;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// Options used by <see cref="IDocumentValidator.ValidateAsync(ValidationOptions)"/>.
    /// </summary>
    public class ValidationOptions
    {
        public ISchema Schema { get; set; } = null!;

        public GraphQLDocument Document { get; set; } = null!;

        public IEnumerable<IValidationRule>? Rules { get; set; }

        public IDictionary<string, object?> UserContext { get; set; } = null!;

        public Inputs Variables { get; set; } = null!;

        public Inputs Extensions { get; set; } = null!;

        /// <summary>
        /// Executed operation.
        /// </summary>
        public GraphQLOperationDefinition Operation { get; set; } = null!;
    }
}
