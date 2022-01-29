using System.Collections.Generic;
using System.Threading;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// Options used by <see cref="IDocumentValidator.ValidateAsync(in ValidationOptions)"/>.
    /// </summary>
    public readonly struct ValidationOptions
    {
        public ISchema Schema { get; init; } = null!;

        public GraphQLDocument Document { get; init; } = null!;

        public IEnumerable<IValidationRule>? Rules { get; init; }

        public IDictionary<string, object?> UserContext { get; init; } = null!;

        public Inputs Variables { get; init; } = null!;

        public Inputs Extensions { get; init; } = null!;

        /// <summary>
        /// Executed operation.
        /// </summary>
        public GraphQLOperationDefinition Operation { get; init; } = null!;

        /// <summary>
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> to cancel validation of request;
        /// defaults to <see cref="CancellationToken.None"/>
        /// </summary>
        public CancellationToken CancellationToken { get; init; }
    }
}
