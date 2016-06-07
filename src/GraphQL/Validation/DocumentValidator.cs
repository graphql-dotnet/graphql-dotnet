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
                var operation = document.Operations.Take(2).Last();

                var error = new ExecutionError("Must provide operation name if query contains multiple operations");
                error.AddLocation(operation.SourceLocation.Line, operation.SourceLocation.Column);

                result.Errors.Add(error);
            }

            return result;
        }
    }
}
