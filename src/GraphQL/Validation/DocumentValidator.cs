using GraphQL.Validation.Rules;

namespace GraphQL.Validation
{
    /// <summary>
    /// Validates a document against a set of validation rules and returns a list of the errors found.
    /// If the document passes validation, also returns the set of parsed variables and argument values for fields and applied directives.
    /// </summary>
    public interface IDocumentValidator
    {
        /// <inheritdoc cref="IDocumentValidator"/>
        Task<IValidationResult> ValidateAsync(in ValidationOptions options);
    }

    /// <inheritdoc/>
    public partial class DocumentValidator : IDocumentValidator
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
        public Task<IValidationResult> ValidateAsync(in ValidationOptions options)
        {
            options.Schema.Initialize();

            var context = Interlocked.Exchange(ref _reusableValidationContext, null) ?? new ValidationContext();
            context.TypeInfo = new TypeInfo(options.Schema);
            context.Schema = options.Schema;
            context.Document = options.Document;
            context.UserContext = options.UserContext;
            context.Variables = options.Variables;
            context.Extensions = options.Extensions;
            context.Operation = options.Operation;
            context.Metrics = options.Metrics;
            context.RequestServices = options.RequestServices;
            context.User = options.User;
            context.CancellationToken = options.CancellationToken;

            return ValidateAsyncCoreAsync(context, options.Rules ?? CoreRules);
        }

        private async Task<IValidationResult> ValidateAsyncCoreAsync(ValidationContext context, IEnumerable<IValidationRule> rules)
        {
            try
            {
                Variables? variables = null;
                List<IVariableVisitor>? variableVisitors = null;

                if (rules.Any())
                {
                    var visitors = new List<INodeVisitor>
                    {
                        context.TypeInfo
                    };

                    foreach (var rule in rules)
                    {
                        var visitor = await rule.GetPreNodeVisitorAsync(context).ConfigureAwait(false);
                        if (visitor != null)
                            visitors.Add(visitor);

                        var variableVisitor = await rule.GetVariableVisitorAsync(context).ConfigureAwait(false);
                        if (variableVisitor != null)
                            (variableVisitors ??= new()).Add(variableVisitor);
                    }

                    await new BasicVisitor(visitors).VisitAsync(context.Document, new BasicVisitor.State(context)).ConfigureAwait(false);
                }

                if (context.HasErrors)
                {
                    return new ValidationResult(context.Errors);
                }

                // can report errors even without rules enabled
                (variables, var errors) = await context.GetVariablesValuesAsync(variableVisitors == null
                    ? null
                    : variableVisitors.Count == 1
                        ? variableVisitors[0]
                        : new CompositeVariableVisitor(variableVisitors)).ConfigureAwait(false);

                if (errors != null)
                {
                    foreach (var error in errors)
                        context.ReportError(error);
                }

                if (context.HasErrors)
                {
                    return new ValidationResult(context.Errors) { Variables = variables };
                }

                // parse all field arguments
                using var parseArgumentVisitorContext = new ParseArgumentVisitor.Context(context, variables);
                await ParseArgumentVisitor.Instance.VisitAsync(context.Document, parseArgumentVisitorContext).ConfigureAwait(false);
                context.ArgumentValues = parseArgumentVisitorContext.ArgumentValues;
                context.DirectiveValues = parseArgumentVisitorContext.DirectiveValues;

                if (context.HasErrors)
                {
                    return new ValidationResult(context.Errors)
                    {
                        Variables = variables,
                        ArgumentValues = context.ArgumentValues,
                        DirectiveValues = context.DirectiveValues,
                    };
                }

                if (rules.Any())
                {
                    List<INodeVisitor>? visitors = null;

                    foreach (var rule in rules)
                    {
                        var visitor = await rule.GetPostNodeVisitorAsync(context).ConfigureAwait(false);
                        if (visitor != null)
                        {
                            visitors ??= [context.TypeInfo];
                            visitors.Add(visitor);
                        }
                    }

                    if (visitors != null)
                    {
                        // clear state of TypeInfo structure to be sure that it is not polluted by previous validation
                        context.TypeInfo.Clear();

                        await new BasicVisitor(visitors).VisitAsync(context.Document, new BasicVisitor.State(context)).ConfigureAwait(false);
                    }
                }

                if (!context.HasErrors && variables == Variables.None && context.ArgumentValues == null && context.DirectiveValues == null)
                    return SuccessfullyValidatedResult.Instance;

                return new ValidationResult(context.Errors)
                {
                    Variables = variables,
                    ArgumentValues = context.ArgumentValues,
                    DirectiveValues = context.DirectiveValues,
                };
            }
            finally
            {
                if (!context.HasErrors)
                {
                    context.Reset();
                    _ = Interlocked.CompareExchange(ref _reusableValidationContext, context, null);
                }
            }
        }
    }
}
