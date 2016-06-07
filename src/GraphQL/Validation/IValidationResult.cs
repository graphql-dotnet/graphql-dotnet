namespace GraphQL.Validation
{
    public interface IValidationResult
    {
        bool IsValid { get; }
        ExecutionErrors Errors { get; set; }
    }
}
