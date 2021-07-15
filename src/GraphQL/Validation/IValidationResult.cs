namespace GraphQL.Validation
{
    /// <summary>
    /// Contains a list of the validation errors found after validating a document against a set of validation rules.
    /// </summary>
    public interface IValidationResult
    {
        /// <summary>
        /// Returns <see langword="true"/> if no errors were found during the validation of a document.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Returns a list of the errors found during validation of a document.
        /// </summary>
        ExecutionErrors Errors { get; }
    }
}
