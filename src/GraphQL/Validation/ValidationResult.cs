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
            Errors.AddRange(errors);
        }

        /// <summary>
        /// Initializes a new instance with the specified set of validation errors.
        /// </summary>
        /// <param name="errors">Set of validation errors.</param>
        public ValidationResult(params ValidationError[] errors)
        {
            Errors.AddRange(errors);
        }

        /// <inheritdoc/>
        public bool IsValid => Errors.Count == 0;

        /// <inheritdoc/>
        public ExecutionErrors Errors { get; } = new ExecutionErrors();
    }

    // Optimization for validation "green path" - does not allocate memory in managed heap.
    /// <summary>
    /// A validation result that indicates no errors were found during validation of the document.
    /// </summary>
    public sealed class SuccessfullyValidatedResult : IValidationResult
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
    }
}
