using System.Collections.Generic;

namespace GraphQL.Validation
{
    public class ValidationResult : IValidationResult
    {
        public ValidationResult(IEnumerable<ValidationError> errors)
        {
            Errors.AddRange(errors);
        }

        public bool IsValid => Errors.Count == 0;

        public ExecutionErrors Errors { get; } = new ExecutionErrors();
    }

    /// <summary>
    /// Optimization for validation "green path" - does not allocate memory in managed heap.
    /// </summary>
    public sealed class SuccessfullyValidatedResult : IValidationResult
    {
        private SuccessfullyValidatedResult() { }

        public static readonly SuccessfullyValidatedResult Instance = new SuccessfullyValidatedResult();

        public bool IsValid => true;

        public ExecutionErrors Errors => EmptyExecutionErrors.Instance;
    }
}
