using System.Linq;
using GraphQL.Language;
using GraphQL.Types;

namespace GraphQL.Validation
{
    public interface IDocumentValidator
    {
        ValidationResult IsValid(ISchema schema, Document document, string operationName);
    }

    public class DocumentValidator : IDocumentValidator
    {
        public ValidationResult IsValid(ISchema schema, Document document, string operationName)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(operationName)
                && document.Operations.Count() > 1)
            {
                result.Errors.Add(new ExecutionError(null, "Must provide operation name if query contains multiple operations"));
            }

            return result;
        }
    }
}
