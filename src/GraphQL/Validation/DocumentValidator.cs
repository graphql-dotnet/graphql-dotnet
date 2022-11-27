using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation.Rules;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation
{
    /// <summary>
    /// Validates a document against a set of validation rules and returns a list of the errors found.
    /// </summary>
    public interface IDocumentValidator
    {
        /// <inheritdoc cref="IDocumentValidator"/>
        Task<(IValidationResult validationResult, Variables variables, IDictionary<GraphQLField, IDictionary<string, ArgumentValue>>? argumentValues, IDictionary<GraphQLField, IDictionary<string, DirectiveInfo>>? directiveValues)> ValidateAsync(in ValidationOptions options);
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
        public Task<(IValidationResult validationResult, Variables variables, IDictionary<GraphQLField, IDictionary<string, ArgumentValue>>? argumentValues, IDictionary<GraphQLField, IDictionary<string, DirectiveInfo>>? directiveValues)> ValidateAsync(in ValidationOptions options)
        {
            options.Schema.Initialize();

            var context = System.Threading.Interlocked.Exchange(ref _reusableValidationContext, null) ?? new ValidationContext();
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

        private async Task<(IValidationResult validationResult, Variables variables, IDictionary<GraphQLField, IDictionary<string, ArgumentValue>>? argumentValues, IDictionary<GraphQLField, IDictionary<string, DirectiveInfo>>? directiveValues)> ValidateAsyncCoreAsync(ValidationContext context, IEnumerable<IValidationRule> rules)
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

                    if (rules is List<IValidationRule> list) //TODO:allocation - optimization for List+Enumerator<IvalidationRule>
                    {
                        foreach (var rule in list)
                        {
                            if (rule is IVariableVisitorProvider provider)
                            {
                                var variableVisitor = provider.GetVisitor(context);
                                if (variableVisitor != null)
                                    (variableVisitors ??= new()).Add(variableVisitor);
                            }
                            var visitor = await rule.ValidateAsync(context).ConfigureAwait(false);
                            if (visitor != null)
                                visitors.Add(visitor);
                        }
                    }
                    else
                    {
                        foreach (var rule in rules)
                        {
                            if (rule is IVariableVisitorProvider provider)
                            {
                                var variableVisitor = provider.GetVisitor(context);
                                if (variableVisitor != null)
                                    (variableVisitors ??= new()).Add(variableVisitor);
                            }
                            var visitor = await rule.ValidateAsync(context).ConfigureAwait(false);
                            if (visitor != null)
                                visitors.Add(visitor);
                        }
                    }
                    await new BasicVisitor(visitors).VisitAsync(context.Document, new BasicVisitor.State(context)).ConfigureAwait(false);
                }

                if (context.HasErrors)
                {
                    return (new ValidationResult(context.Errors), Variables.None, null, null);
                }

                // can report errors even without rules enabled
                variables = await context.GetVariableValuesAsync(variableVisitors == null ? null : variableVisitors.Count == 1 ? variableVisitors[0] : new CompositeVariableVisitor(variableVisitors)).ConfigureAwait(false);

                if (context.HasErrors)
                {
                    return (new ValidationResult(context.Errors), variables, null, null);
                }

                // parse all field arguments
                using var parseArgumentVisitorContext = new ParseArgumentVisitor.Context(context, variables);
                await ParseArgumentVisitor.Instance.VisitAsync(context.Document, parseArgumentVisitorContext).ConfigureAwait(false);
                var argumentValues = parseArgumentVisitorContext.ArgumentValues;
                var directiveValues = parseArgumentVisitorContext.DirectiveValues;

                // todo: execute validation rules that need to be able to read field arguments/directives

                return context.HasErrors
                    ? (new ValidationResult(context.Errors), variables, argumentValues, directiveValues)
                    : (SuccessfullyValidatedResult.Instance, variables, argumentValues, directiveValues);
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

        private class ParseArgumentVisitor : ASTVisitor<ParseArgumentVisitor.Context>
        {
            public static readonly ParseArgumentVisitor Instance = new();

            private ParseArgumentVisitor() { }

            protected override async ValueTask VisitOperationDefinitionAsync(GraphQLOperationDefinition operationDefinition, Context context)
            {
                var type = operationDefinition.Operation switch
                {
                    OperationType.Query => context.Schema.Query,
                    OperationType.Mutation => context.Schema.Mutation,
                    OperationType.Subscription => context.Schema.Subscription,
                    _ => null
                };
                if (type == null)
                    return;
                context.Types.Push(type);
                await base.VisitOperationDefinitionAsync(operationDefinition, context).ConfigureAwait(false);
                context.Types.Pop();
            }

            protected override async ValueTask VisitInlineFragmentAsync(GraphQLInlineFragment inlineFragment, Context context)
            {
                var typeCondition = inlineFragment.TypeCondition;
                if (typeCondition == null)
                {
                    await base.VisitInlineFragmentAsync(inlineFragment, context).ConfigureAwait(false);
                }
                else
                {
                    if (context.Schema.AllTypes[typeCondition.Type.Name.Value]?.GetNamedType() is IComplexGraphType type)
                    {
                        context.Types.Push(type);
                        await base.VisitInlineFragmentAsync(inlineFragment, context).ConfigureAwait(false);
                        context.Types.Pop();
                    }
                }
            }

            protected override async ValueTask VisitFragmentDefinitionAsync(GraphQLFragmentDefinition fragmentDefinition, Context context)
            {
                if (context.Schema.AllTypes[fragmentDefinition.TypeCondition.Type.Name.Value]?.GetNamedType() is IComplexGraphType type)
                {
                    context.Types.Push(type);
                    await base.VisitFragmentDefinitionAsync(fragmentDefinition, context).ConfigureAwait(false);
                    context.Types.Pop();
                }
            }

            protected override async ValueTask VisitFieldAsync(GraphQLField field, Context context)
            {
                var fieldType = field.Name.Value == context.Schema.TypeMetaFieldType.Name
                    ? context.Schema.TypeMetaFieldType
                    : context.Type?.Fields.Find(field.Name.Value);
                if (fieldType == null)
                    return;
                if (field.Arguments?.Count > 0)
                {
                    try
                    {
                        var arguments = ExecutionHelper.GetArguments(fieldType.Arguments, field.Arguments, context.Variables);
                        if (arguments != null)
                        {
                            (context.ArgumentValues ??= new()).Add(field, arguments);
                        }
                    }
                    catch (Exception ex)
                    {
                        // todo: report error properly
                        context.ValidationContext.ReportError(new ValidationError($"Error trying to resolve field '{field.Name.Value}'.", ex));
                    }
                }
                if (field.Directives?.Count > 0)
                {
                    try
                    {
                        var directives = ExecutionHelper.GetDirectives(field, context.Variables, context.Schema);
                        if (directives != null)
                        {
                            (context.DirectiveValues ??= new()).Add(field, directives);
                        }
                    }
                    catch (Exception ex)
                    {
                        // todo: report error properly
                        context.ValidationContext.ReportError(new ValidationError($"Error trying to resolve field '{field.Name.Value}'.", ex));
                    }
                }
                if (fieldType.ResolvedType?.GetNamedType() is IComplexGraphType type)
                {
                    context.Types.Push(type);
                    await base.VisitFieldAsync(field, context).ConfigureAwait(false);
                    context.Types.Pop();
                }
            }

            public class Context : IASTVisitorContext, IDisposable
            {
                public ISchema Schema => ValidationContext.Schema;
                public ValidationContext ValidationContext { get; set; }
                public Stack<IComplexGraphType> Types { get; set; }
                public IComplexGraphType? Type => Types.Count > 0 ? Types.Peek() : null;
                public Variables Variables { get; set; }
                public CancellationToken CancellationToken => ValidationContext.CancellationToken;
                public Dictionary<GraphQLField, IDictionary<string, ArgumentValue>>? ArgumentValues { get; set; }
                public Dictionary<GraphQLField, IDictionary<string, DirectiveInfo>>? DirectiveValues { get; set; }

                private static Stack<IComplexGraphType>? _reusableTypes;

                public Context(ValidationContext validationContext, Variables variables)
                {
                    Types = Interlocked.Exchange(ref _reusableTypes, null) ?? new();
                    ValidationContext = validationContext;
                    Variables = variables;
                }

                public void Dispose()
                {
                    Types.Clear();
                    Interlocked.CompareExchange(ref _reusableTypes, Types, null);
                    Types = null!;
                }
            }
        }
    }
}
