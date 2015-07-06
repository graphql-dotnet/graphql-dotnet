using System.Linq;

namespace GraphQL
{
    public class DocumentValidator
    {
        public ValidationResult IsValid(Schema schema, Document document, string operationName)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(operationName)
                && document.Operations.Count() > 1)
            {
                result.Errors.Add(new ExecutionError("Must provide operation name if query contains multiple operations"));
            }

            return result;
        }
    }

    public class ValidationResult
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