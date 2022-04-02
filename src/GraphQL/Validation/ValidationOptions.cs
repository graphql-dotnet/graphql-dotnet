using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// Options used by <see cref="IDocumentValidator.ValidateAsync(in ValidationOptions)"/>.
    /// </summary>
    public readonly struct ValidationOptions
    {
        /// <summary>
        /// Creates a default instance of <see cref="ValidationOptions"/>.
        /// </summary>
        public ValidationOptions()
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="ISchema"/> to validate the <see cref="GraphQLDocument"/> against.
        /// </summary>
        public ISchema Schema { get; init; } = null!;

        /// <summary>
        /// Gets or sets the <see cref="GraphQLDocument"/> to validate.
        /// </summary>
        public GraphQLDocument Document { get; init; } = null!;

        /// <summary>
        /// Gets or sets a list of rules to use to validate the <see cref="GraphQLDocument"/>.
        /// If no rules are specified, <see cref="DocumentValidator.CoreRules"/> are used.
        /// </summary>
        public IEnumerable<IValidationRule>? Rules { get; init; } = null;

        /// <summary>
        /// Gets or sets the user context, which can be used by validation rules
        /// during document validation.
        /// </summary>
        public IDictionary<string, object?> UserContext { get; init; } = null!;

        /// <summary>
        /// Gets or sets the input variables.
        /// </summary>
        public Inputs Variables { get; init; } = null!;

        /// <summary>
        /// Gets or sets the input extensions.
        /// </summary>
        public Inputs Extensions { get; init; } = null!;

        /// <summary>
        /// Executed operation.
        /// </summary>
        public GraphQLOperationDefinition Operation { get; init; } = null!;

        /// <inheritdoc cref="ExecutionOptions.RequestServices"/>
        public IServiceProvider? RequestServices { get; init; } = null;

        /// <summary>
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> to cancel validation of request;
        /// defaults to <see cref="CancellationToken.None"/>
        /// </summary>
        public CancellationToken CancellationToken { get; init; } = default;
    }
}
