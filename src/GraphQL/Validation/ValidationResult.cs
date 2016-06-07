using System.Linq;

namespace GraphQL.Validation
{
    public class ValidationResult : IValidationResult
    {
        public ValidationResult()
        {
            Errors = new ExecutionErrors();
        }

        public bool IsValid
        {
            get { return !Errors.Any(); }
        }

        public ExecutionErrors Errors { get; set; }
    }
}
