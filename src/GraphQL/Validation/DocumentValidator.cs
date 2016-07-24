using System.Collections.Generic;
using System.Linq;
using GraphQL.Language;
using GraphQL.Types;
using GraphQL.Validation.Rules;

namespace GraphQL.Validation
{
    public interface IDocumentValidator
    {
        IValidationResult Validate(
            ISchema schema,
            Document document,
            IEnumerable<IValidationRule> rules = null);
    }

    public class DocumentValidator : IDocumentValidator
    {
        public IValidationResult Validate(
            ISchema schema,
            Document document,
            IEnumerable<IValidationRule> rules = null)
        {
            var context = new ValidationContext
            {
                Schema = schema,
                Document = document,
                TypeInfo = new TypeInfo(schema)
            };

            if (rules == null)
            {
                rules = CoreRules();
            }

            var visitors = rules.Select(x => x.Validate(context)).ToList();

            visitors.Insert(0, context.TypeInfo);
#if DEBUG
            visitors.Insert(1, new DebugNodeVisitor());
#endif

            var basic = new BasicVisitor(visitors.ToArray());

            basic.Visit(document);

            var result = new ValidationResult();
            result.Errors.AddRange(context.Errors);
            return result;
        }

        public static List<IValidationRule> CoreRules()
        {
            var rules = new List<IValidationRule>
            {
                new ArgumentsOfCorrectType(),
                new UniqueOperationNames(),
                new LoneAnonymousOperation(),
                new NoUndefinedVariables(),
                new ScalarLeafs(),
                new UniqueInputFieldNames(),
                new VariablesAreInputTypes(),
                new VariablesInAllowedPosition()
            };
            return rules;
        }
    }
}
