using System.Collections.Generic;
using System.Linq;
using GraphQL.Language;
using GraphQL.Types;
using GraphQL.Validation.Rules;

namespace GraphQL.Validation
{
    public interface IDocumentValidator
    {
        List<IValidationRule> Rules(Schema schema, Document document);
        IValidationResult IsValid(Schema schema, Document document, string operationName);
    }

    public class DocumentValidator : IDocumentValidator
    {
        public List<IValidationRule> Rules(Schema schema, Document docoment)
        {
            List<IValidationRule> rules = new List<IValidationRule>()
            {
                new OperationNameUniquenessRule(),
                new LoneAnonymousOperationRule()
            };
            return rules;
        }

        public IValidationResult IsValid(Schema schema, Document document, string operationName)
        {
            var result = new ValidationResult();
            var rules = Rules(schema, document);

            List<ExecutionError> errors = new List<ExecutionError>(rules.Count);

            foreach (IValidationRule rule in rules)
            {
                List<ExecutionError> newErrors = rule.Validate(schema, document, operationName);
                if (newErrors != null)
                    errors.AddRange(newErrors);
            }

            return result;
        }
    }
}
