using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation.Rules;

namespace GraphQL.Validation
{
    /// <summary>
    /// Validates a document against a set of validation rules and returns a list of the errors found.
    /// </summary>
    public interface IDocumentValidator
    {
        /// <inheritdoc cref="IDocumentValidator"/>
        Task<IValidationResult> ValidateAsync(
            string originalQuery,
            ISchema schema,
            Document document,
            IEnumerable<IValidationRule> rules = null,
            IDictionary<string, object> userContext = null,
            Inputs inputs = null);
    }

    /// <inheritdoc/>
    public class DocumentValidator : IDocumentValidator
    {
        /// <summary>
        /// Returns the default set of validation rules: all except <see cref="OverlappingFieldsCanBeMerged"/>.
        /// </summary>
        public static readonly IEnumerable<IValidationRule> CoreRules = new List<IValidationRule>
        {
            UniqueOperationNames.Instance,
            LoneAnonymousOperation.Instance,
            SingleRootFieldSubscriptions.Instance,
            KnownTypeNames.Instance,
            FragmentsOnCompositeTypes.Instance,
            VariablesAreInputTypes.Instance,
            ScalarLeafs.Instance,
            FieldsOnCorrectType.Instance,
            UniqueFragmentNames.Instance,
            KnownFragmentNames.Instance,
            NoUnusedFragments.Instance,
            PossibleFragmentSpreads.Instance,
            NoFragmentCycles.Instance,
            NoUndefinedVariables.Instance,
            NoUnusedVariables.Instance,
            UniqueVariableNames.Instance,
            KnownDirectives.Instance,
            UniqueDirectivesPerLocation.Instance,
            KnownArgumentNames.Instance,
            UniqueArgumentNames.Instance,
            ArgumentsOfCorrectType.Instance,
            ProvidedNonNullArguments.Instance,
            DefaultValuesOfCorrectType.Instance,
            VariablesInAllowedPosition.Instance,
            UniqueInputFieldNames.Instance,
            //OverlappingFieldsCanBeMerged.Instance uncomment later
        }.AsReadOnly();

        /// <inheritdoc/>
        public async Task<IValidationResult> ValidateAsync(
            string originalQuery,
            ISchema schema,
            Document document,
            IEnumerable<IValidationRule> rules = null,
            IDictionary<string, object> userContext = null,
            Inputs inputs = null)
        {
            if (!schema.Initialized)
            {
                schema.Initialize();
            }

            var context = new ValidationContext
            {
                OriginalQuery = originalQuery,
                Schema = schema,
                Document = document,
                TypeInfo = new TypeInfo(schema),
                UserContext = userContext,
                Inputs = inputs
            };

            List<INodeVisitor> visitors;

            if (rules == null)
            {
                // Optimizations for standard rules:
                //   1. Known size of allocated list.
                //   2. No LINQ related allocations.
                //   3. No async/await related allocations since all standard rules return cached tasks from ValidateAsync.
                var coreRules = (ReadOnlyCollection<IValidationRule>)CoreRules;
                visitors = new List<INodeVisitor>(coreRules.Count + 1) { context.TypeInfo };
                for (int i = 0; i < coreRules.Count; ++i)
                    visitors.Add(coreRules[i].ValidateAsync(context).Result);
            }
            else
            {
                visitors = (await Task.WhenAll(rules.Select(x => x.ValidateAsync(context)))).ToList();
                visitors.Insert(0, context.TypeInfo);
            }

            var basic = new BasicVisitor(visitors);

            basic.Visit(document, context);

            if (context.HasErrors)
                return new ValidationResult(context.Errors);

            return SuccessfullyValidatedResult.Instance;
        }
    }
}
