using GraphQL.Execution;
using GraphQLParser.AST;

namespace GraphQL.Validation
{
    /// <inheritdoc cref="IValidationResult"/>
    public class ValidationResult : IValidationResult
    {
        /// <summary>
        /// Initializes a new instance with the specified set of validation errors.
        /// </summary>
        /// <param name="errors">Set of validation errors.</param>
        public ValidationResult(IEnumerable<ValidationError> errors)
        {
            if (errors.Any())
            {
                Errors.AddRange(errors);
            }
        }

        /// <summary>
        /// Initializes a new instance with the specified set of validation errors.
        /// </summary>
        /// <param name="errors">Set of validation errors.</param>
        public ValidationResult(params ValidationError[] errors)
            : this((IEnumerable<ValidationError>)errors)
        {
        }

        /// <inheritdoc/>
        public bool IsValid => (_errors?.Count ?? 0) == 0;

        private ExecutionErrors? _errors;

        /// <inheritdoc/>
        public ExecutionErrors Errors => _errors ??= new ExecutionErrors();

        /// <inheritdoc/>
        public Variables? Variables { get; set; }

        /// <inheritdoc/>
        public IDictionary<GraphQLField, IDictionary<string, ArgumentValue>>? ArgumentValues { get; set; }

        /// <inheritdoc/>
        public IDictionary<GraphQLField, IDictionary<string, DirectiveInfo>>? DirectiveValues { get; set; }
    }

    // Optimization for validation "green path" - does not allocate memory in managed heap.
    /// <summary>
    /// A validation result that indicates no errors were found during validation of the document.
    /// </summary>
    internal sealed class SuccessfullyValidatedResult : IValidationResult
    {
        private SuccessfullyValidatedResult() { }

        /// <summary>
        /// Returns a static instance of this class.
        /// </summary>
        public static readonly IValidationResult Instance = new SuccessfullyValidatedResult();

        /// <summary>
        /// Returns <see langword="true"/> indicating that the document was successfully validated.
        /// </summary>
        public bool IsValid => true;

        /// <summary>
        /// Returns an empty list of execution errors.
        /// </summary>
        public ExecutionErrors Errors => EmptyExecutionErrors.Instance;

        /// <inheritdoc/>
        public Variables? Variables => null;

        /// <inheritdoc/>
        public IDictionary<GraphQLField, IDictionary<string, ArgumentValue>>? ArgumentValues => null;

        /// <inheritdoc/>
        public IDictionary<GraphQLField, IDictionary<string, DirectiveInfo>>? DirectiveValues => null;
    }
}
