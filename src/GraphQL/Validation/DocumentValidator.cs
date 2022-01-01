using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Rules;

namespace GraphQL.Validation
{
    /// <summary>
    /// Validates a document against a set of validation rules and returns a list of the errors found.
    /// </summary>
    public interface IDocumentValidator
    {
        /// <inheritdoc cref="IDocumentValidator"/>
        Task<(IValidationResult validationResult, Variables variables)> ValidateAsync(ValidationOptions options);
    }

    /// <inheritdoc/>
    public class DocumentValidator : IDocumentValidator
    {
        // frequently reused objects
        private ValidationContext? _reusableValidationContext;

        /// <summary>
        /// Returns the default set of validation rules.
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
            KnownDirectivesInAllowedLocations.Instance,
            UniqueDirectivesPerLocation.Instance,
            KnownArgumentNames.Instance,
            UniqueArgumentNames.Instance,
            ArgumentsOfCorrectType.Instance,
            ProvidedNonNullArguments.Instance,
            DefaultValuesOfCorrectType.Instance,
            VariablesInAllowedPosition.Instance,
            UniqueInputFieldNames.Instance,
            OverlappingFieldsCanBeMerged.Instance,
        };

        /// <inheritdoc/>
        public async Task<(IValidationResult validationResult, Variables variables)> ValidateAsync(ValidationOptions options)
        {
            options.Schema.Initialize();

            var context = System.Threading.Interlocked.Exchange(ref _reusableValidationContext, null) ?? new ValidationContext();
            context.Schema = options.Schema;
            context.Document = options.Document;
            context.TypeInfo = new TypeInfo(options.Schema);
            context.UserContext = options.UserContext;
            context.Variables = options.Variables;
            context.Extensions = options.Extensions;
            context.OperationName = options.OperationName;

            var rules = options.Rules ?? CoreRules;
            try
            {
                Variables? variablesObj = null;

                if (!rules.Any())
                {
                    variablesObj = context.GetVariableValues(options.VariableDefinitions); // can report errors even without rules enabled
                }
                else
                {
                    var visitors = new List<INodeVisitor>
                    {
                        context.TypeInfo
                    };
                    List<IVariableVisitor>? variableVisitors = null;

                    foreach (var rule in rules)
                    {
                        if (rule is IVariableVisitorProvider provider)
                        {
                            var variableVisitor = provider.GetVisitor(context);
                            if (variableVisitor != null)
                                (variableVisitors ??= new List<IVariableVisitor>()).Add(variableVisitor);
                        }
                        var visitor = await rule.ValidateAsync(context).ConfigureAwait(false);
                        if (visitor != null)
                            visitors.Add(visitor);
                    }

                    new BasicVisitor(visitors).Visit(context.Document, context);

                    variablesObj = context.GetVariableValues(options.VariableDefinitions,
                        variableVisitors == null ? null : variableVisitors.Count == 1 ? variableVisitors[0] : new CompositeVariableVisitor(variableVisitors));
                }

                return context.HasErrors
                    ? (new ValidationResult(context.Errors), variablesObj)
                    : (SuccessfullyValidatedResult.Instance, variablesObj);
            }
            finally
            {
                if (!context.HasErrors)
                {
                    context.Reset();
                    _ = System.Threading.Interlocked.CompareExchange(ref _reusableValidationContext, context, null);
                }
            }
        }
    }
}
