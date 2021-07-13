#nullable enable

using System.Collections.Generic;
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
        Task<(IValidationResult validationResult, Variables variables)> ValidateAsync(
            ISchema schema,
            Document document,
            VariableDefinitions? variableDefinitions,
            IEnumerable<IValidationRule>? rules = null,
            IDictionary<string, object?> userContext = null!,
            Inputs? inputs = null);
    }

    /// <inheritdoc/>
    public class DocumentValidator : IDocumentValidator
    {
        // frequently reused objects
        private ValidationContext? _reusableValidationContext;

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
            KnownDirectivesInAllowedLocations.Instance,
            UniqueDirectivesPerLocation.Instance,
            KnownArgumentNames.Instance,
            UniqueArgumentNames.Instance,
            ArgumentsOfCorrectType.Instance,
            ProvidedNonNullArguments.Instance,
            DefaultValuesOfCorrectType.Instance,
            VariablesInAllowedPosition.Instance,
            UniqueInputFieldNames.Instance
        };

        /// <inheritdoc/>
        public async Task<(IValidationResult validationResult, Variables variables)> ValidateAsync(
            ISchema schema,
            Document document,
            VariableDefinitions? variableDefinitions,
            IEnumerable<IValidationRule>? rules = null,
            IDictionary<string, object?> userContext = null!,
            Inputs? inputs = null)
        {
            schema.Initialize();

            var context = System.Threading.Interlocked.Exchange(ref _reusableValidationContext, null) ?? new ValidationContext();
            context.Schema = schema;
            context.Document = document;
            context.TypeInfo = new TypeInfo(schema);
            context.UserContext = userContext;
            context.Inputs = inputs;

            try
            {
                Variables? variables = null;
                bool useOnlyStandardRules = rules == null;

                rules ??= CoreRules;

                if (!rules.Any())
                {
                    variables = context.GetVariableValues(schema, variableDefinitions, inputs ?? Inputs.Empty); // can report errors even without rules enabled
                }
                else
                {
                    List<INodeVisitor> visitors;
                    List<IVariableVisitor>? variableVisitors = null;

                    if (useOnlyStandardRules) // standard rules don't validate variables
                    {
                        // No async/await related allocations since all standard rules return completed tasks from ValidateAsync.
                        visitors = new List<INodeVisitor>();
                        foreach (var rule in (List<IValidationRule>)rules) // no iterator boxing
                        {
                            var visitor = rule.ValidateAsync(context)?.Result;
                            if (visitor != null)
                                visitors.Add(visitor);
                        }
                    }
                    else
                    {
                        var awaitedVisitors = rules.Select(rule =>
                        {
                            if (rule is IVariableVisitorProvider provider)
                            {
                                var variableVisitor = provider.GetVisitor(context);
                                if (variableVisitor != null)
                                    (variableVisitors ??= new List<IVariableVisitor>()).Add(variableVisitor);
                            }
                            return rule.ValidateAsync(context);
                        }).Where(x => x != null);
                        visitors = (await Task.WhenAll(awaitedVisitors)).ToList();
                    }

                    visitors.Insert(0, context.TypeInfo);

                    new BasicVisitor(visitors).Visit(document, context);

                    variables = context.GetVariableValues(schema, variableDefinitions, inputs ?? Inputs.Empty,
                        variableVisitors == null ? null : variableVisitors.Count == 1 ? variableVisitors[0] : new CompositeVariableVisitor(variableVisitors));
                }

                return context.HasErrors
                    ? (new ValidationResult(context.Errors), variables)
                    : (SuccessfullyValidatedResult.Instance, variables);
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
