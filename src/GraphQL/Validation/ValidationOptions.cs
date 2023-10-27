using System.Security.Claims;
using GraphQL.Instrumentation;
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
        /// Creates a default instance of <see cref="ValidationOptions"/> with the specified options.
        /// </summary>
        public ValidationOptions(
            ISchema schema,
            GraphQLDocument document,
            IEnumerable<IValidationRule>? rules,
            IDictionary<string, object?> userContext,
            Metrics metrics,
            Inputs variables,
            Inputs extensions,
            GraphQLOperationDefinition operation,
            IServiceProvider? requestServices,
            ClaimsPrincipal? user,
            CancellationToken cancellationToken)
        {
            // this constructor is required for C# 8.0 and prior consumers, as they cannot write to init-only properties
            Schema = schema;
            Document = document;
            Rules = rules;
            UserContext = userContext;
            Metrics = metrics;
            Variables = variables;
            Extensions = extensions;
            Operation = operation;
            RequestServices = requestServices;
            User = user;
            CancellationToken = cancellationToken;
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
        /// Gets or sets object for performance metrics, which can be used by
        /// validation rules during document validation.
        /// </summary>
        public Metrics Metrics { get; init; } = null!;

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

        /// <inheritdoc cref="ExecutionOptions.User"/>
        public ClaimsPrincipal? User { get; init; } = null;

        /// <summary>
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see> to cancel validation of request;
        /// defaults to <see cref="CancellationToken.None"/>
        /// </summary>
        public CancellationToken CancellationToken { get; init; } = default;
    }
}
