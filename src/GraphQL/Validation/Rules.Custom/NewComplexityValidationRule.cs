using System.Diagnostics;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation.Complexity;
using GraphQL.Validation.Errors.Custom;
using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation.Rules.Custom;

/// <summary>
/// Analyzes a document to determine if its complexity exceeds a threshold.
/// </summary>
public class NewComplexityValidationRule : ValidationRuleBase
{
    /// <inheritdoc cref="NewComplexityConfiguration"/>
    protected NewComplexityConfiguration ComplexityConfiguration { get; }

    /// <summary>
    /// Initializes an instance with the specified complexity configuration.
    /// </summary>
    public NewComplexityValidationRule(NewComplexityConfiguration complexityConfiguration)
    {
        ComplexityConfiguration = complexityConfiguration;
    }

    /// <inheritdoc/>
    public override async ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context)
    {
        // Fast return here to avoid all possible problems with complexity analysis.
        // For example, document may contain fragment cycles, see https://github.com/graphql-dotnet/graphql-dotnet/issues/3527
        if (!context.HasErrors)
            using (context.Metrics.Subject("document", "Analyzing complexity"))
            {
                var complexity = await ComplexityVisitor.RunAsync(context, ComplexityConfiguration).ConfigureAwait(false);
                await ValidateComplexityAsync(context, complexity.TotalComplexity, complexity.MaximumDepth).ConfigureAwait(false);
            }
        return default;
    }

    /// <summary>
    /// Determines if the computed complexity exceeds the configured threshold.
    /// </summary>
    protected virtual Task ValidateComplexityAsync(ValidationContext context, double totalComplexity, double maxDepth)
    {
        if (totalComplexity > ComplexityConfiguration.MaxComplexity)
            context.ReportError(new ComplexityError(
                $"Query is too complex to execute. Complexity is {totalComplexity}; maximum allowed on this endpoint is {ComplexityConfiguration.MaxComplexity}."));

        if (maxDepth > ComplexityConfiguration.MaxDepth)
            context.ReportError(new ComplexityError(
                $"Query is too nested to execute. Maximum depth is {maxDepth} levels; maximum allowed on this endpoint is {ComplexityConfiguration.MaxDepth}."));

        return Task.CompletedTask;
    }

    private sealed class ComplexityVisitor : ASTVisitor<VisitorContext>
    {
        private ComplexityVisitor()
        {
        }

        private static readonly ComplexityVisitor _instance = new();

        public static async ValueTask<(double TotalComplexity, double MaximumDepth)> RunAsync(
            ValidationContext validationContext,
            NewComplexityConfiguration complexityConfiguration)
        {
            using var context = new VisitorContext(validationContext, complexityConfiguration);
            await _instance.VisitAsync(validationContext.Operation, context).ConfigureAwait(false);
            return (context.TotalComplexity, context.MaximumDepth);
        }

        protected override ValueTask VisitOperationDefinitionAsync(GraphQLOperationDefinition operationDefinition, VisitorContext context)
        {
            context.LastType = operationDefinition.Operation switch
            {
                OperationType.Query => context.Schema.Query,
                OperationType.Mutation => context.Schema.Mutation,
                OperationType.Subscription => context.Schema.Subscription,
                _ => ThrowInvalidOperation(operationDefinition.Operation),
            };
            if (context.LastType == null)
                ThrowOperationNotDefined(operationDefinition.Operation);
            return base.VisitAsync(operationDefinition.SelectionSet.Selections, context);

            [StackTraceHidden, DoesNotReturn]
            static IObjectGraphType ThrowInvalidOperation(OperationType operationType)
                => throw new InvalidOperationException($"Unknown operation type: {operationType}");

            [StackTraceHidden, DoesNotReturn]
            static void ThrowOperationNotDefined(OperationType operationType)
                => throw new InvalidOperationException($"Schema is not configured for operation type: {operationType}");
        }

        protected override ValueTask VisitFragmentDefinitionAsync(GraphQLFragmentDefinition fragmentDefinition, VisitorContext context)
        {
            var typeName = fragmentDefinition.TypeCondition.Type.Name;
            context.LastType = context.Schema.AllTypes[typeName.Value]
                ?? ThrowInvalidType(typeName);
            return base.VisitFragmentDefinitionAsync(fragmentDefinition, context);

            [StackTraceHidden, DoesNotReturn]
            static IGraphType ThrowInvalidType(GraphQLName typeName)
                => throw new InvalidOperationException($"Type '{typeName.StringValue}' not found in schema.");
        }

        protected override ValueTask VisitInlineFragmentAsync(GraphQLInlineFragment inlineFragment, VisitorContext context)
        {
            if (!context.ShouldIncludeNode(inlineFragment))
                return default;
            if (inlineFragment.TypeCondition != null)
            {
                var typeName = inlineFragment.TypeCondition.Type.Name;
                context.LastType = context.Schema.AllTypes[typeName.Value]
                    ?? ThrowInvalidType(typeName);
            }
            return base.VisitInlineFragmentAsync(inlineFragment, context);

            [StackTraceHidden, DoesNotReturn]
            static IGraphType ThrowInvalidType(GraphQLName typeName)
                => throw new InvalidOperationException($"Type '{typeName.StringValue}' not found in schema.");
        }

        protected override async ValueTask VisitFragmentSpreadAsync(GraphQLFragmentSpread fragmentSpread, VisitorContext context)
        {
            if (!context.ShouldIncludeNode(fragmentSpread))
                return;
            var fragmentName = fragmentSpread.FragmentName.Name;
            var fragment = context.Document.FindFragmentDefinition(fragmentName.Value)
                ?? ThrowInvalidFragment(fragmentName);
            if (context.FragmentsProcessed.Contains(fragment))
                ThrowCircularReference(fragmentName);
            context.FragmentsProcessed.Push(fragment);
            await base.VisitFragmentDefinitionAsync(fragment, context).ConfigureAwait(false);
            context.FragmentsProcessed.Pop();

            [StackTraceHidden, DoesNotReturn]
            static GraphQLFragmentDefinition ThrowInvalidFragment(GraphQLName fragmentName)
                => throw new InvalidOperationException($"Fragment '{fragmentName.StringValue}' not found in document.");

            [StackTraceHidden, DoesNotReturn]
            static InvalidOperationException ThrowCircularReference(GraphQLName fragmentName)
                => throw new InvalidOperationException($"Fragment '{fragmentName.StringValue}' has a circular reference.");
        }

        protected override ValueTask VisitFieldAsync(GraphQLField field, VisitorContext context)
        {
            if (!context.ShouldIncludeNode(field))
                return default;
            FieldType fieldType;
            if (field.Name.Value == "__typename")
            {
                fieldType = context.Schema.TypeNameMetaFieldType;
            }
            else if (context.LastType == context.Schema.Query && field.Name.Value == "__schema")
            {
                fieldType = context.Schema.SchemaMetaFieldType;
            }
            else if (context.LastType == context.Schema.Query && field.Name.Value == "__type")
            {
                fieldType = context.Schema.TypeMetaFieldType;
            }
            else
            {
                if (context.LastType is not IComplexGraphType objectGraphType)
                    ThrowNotObjectType(context.LastType);
                fieldType = objectGraphType.GetField(field.Name.Value)
                    ?? ThrowFieldNotFound(field, objectGraphType);
            }
            var complexityImpactFunc = fieldType.GetComplexityImpactFunc()
                ?? context.Configuration.DefaultComplexityImpactFunc;
            var complexityImpact = complexityImpactFunc(new FieldImpactContext()
            {
                FieldDefinition = fieldType,
                FieldAst = field,
                ParentType = context.LastType!,
                VisitorContext = context,
            });
            context.TotalComplexity += complexityImpact.FieldImpact * context.StandingComplexity;
            return field.SelectionSet != null /* even if ChildImpactMultiplier is 0, we still need to visit children to determine the max depth */
                ? VisitChildrenAsync(complexityImpact.ChildImpactMultiplier)
                : default;

            async ValueTask VisitChildrenAsync(double multiplier)
            {
                context.FieldAsts.Push(field);
                context.FieldDefinitions.Push(fieldType);
                context.ParentTypes.Push(context.LastType!);
                context.TotalDepth++;
                context.MaximumDepth = Math.Max(context.MaximumDepth, context.TotalDepth);
                var oldMultiplier = context.StandingComplexity;
                context.StandingComplexity *= multiplier;
                await VisitAsync(field.SelectionSet.Selections, context).ConfigureAwait(false);
                context.StandingComplexity = oldMultiplier;
                context.TotalDepth--;
                context.ParentTypes.Pop();
                context.FieldDefinitions.Pop();
                context.FieldAsts.Pop();
            }

            [StackTraceHidden, DoesNotReturn]
            static void ThrowNotObjectType(IGraphType? type)
                => throw new InvalidOperationException($"Type '{type?.Name}' is not an object type.");

            [StackTraceHidden, DoesNotReturn]
            static FieldType ThrowFieldNotFound(GraphQLField field, IComplexGraphType type)
                => throw new InvalidOperationException($"Field '{field.Name.StringValue}' not found in type '{type.Name}'.");
        }
    }

    internal sealed class VisitorContext : IASTVisitorContext, IDisposable
    {
        private static Stack<GraphQLField>? _sharedFieldAsts;
        private static Stack<FieldType>? _sharedFieldDefinitions;
        private static Stack<GraphQLFragmentDefinition>? _sharedFragmentsProcessed;
        private static Stack<IGraphType>? _sharedParentTypes;
        public VisitorContext(ValidationContext validationContext, NewComplexityConfiguration complexityConfiguration)
        {
            ValidationContext = validationContext;
            Configuration = complexityConfiguration;
        }
        public readonly Stack<GraphQLField> FieldAsts = Interlocked.Exchange(ref _sharedFieldAsts, null) ?? new();
        public readonly Stack<FieldType> FieldDefinitions = Interlocked.Exchange(ref _sharedFieldDefinitions, null) ?? new();
        public readonly Stack<GraphQLFragmentDefinition> FragmentsProcessed = Interlocked.Exchange(ref _sharedFragmentsProcessed, null) ?? new();
        public readonly Stack<IGraphType> ParentTypes = Interlocked.Exchange(ref _sharedParentTypes, null) ?? new();
        public readonly ValidationContext ValidationContext;
        public readonly NewComplexityConfiguration Configuration;
        public IGraphType? LastType;
        public double TotalComplexity;
        public double StandingComplexity = 1;
        public int TotalDepth = 1;
        public int MaximumDepth = 1;

        public IDictionary<GraphQLField, IDictionary<string, ArgumentValue>>? Arguments => ValidationContext.ArgumentValues;
        public ISchema Schema => ValidationContext.Schema;
        public GraphQLDocument Document => ValidationContext.Document;
        public GraphQLOperationDefinition Operation => ValidationContext.Operation;
        public Func<ASTNode, bool> ShouldIncludeNode => ValidationContext.ShouldIncludeNode;
        public CancellationToken CancellationToken => default;

        public void Dispose()
        {
            FieldAsts.Clear();
            FieldDefinitions.Clear();
            FragmentsProcessed.Clear();
            ParentTypes.Clear();
            Interlocked.Exchange(ref _sharedFieldAsts, FieldAsts);
            Interlocked.Exchange(ref _sharedFieldDefinitions, FieldDefinitions);
            Interlocked.Exchange(ref _sharedFragmentsProcessed, FragmentsProcessed);
            Interlocked.Exchange(ref _sharedParentTypes, ParentTypes);
        }
    }

    /// <summary>
    /// Provides contextual information about the field being analyzed.
    /// </summary>
    public struct FieldImpactContext
    {
        /// <inheritdoc cref="IResolveFieldContext.FieldDefinition"/>
        public FieldType FieldDefinition { get; set; }

        /// <inheritdoc cref="IResolveFieldContext.FieldAst"/>
        public GraphQLField FieldAst { get; set; }

        /// <inheritdoc cref="IResolveFieldContext.ParentType"/>
        public IGraphType ParentType { get; set; }

        internal VisitorContext VisitorContext;
        private int _parentDepth;

        private IDictionary<string, ArgumentValue>? _arguments;
        /// <inheritdoc cref="IResolveFieldContext.Arguments"/>
        public IDictionary<string, ArgumentValue>? Arguments => _arguments ??= (VisitorContext.Arguments?.TryGetValue(FieldAst, out var args) == true ? args : null);

        /// <inheritdoc cref="ResolveFieldContextExtensions.GetArgument{TType}(IResolveFieldContext, string, TType)"/>
        public TType GetArgument<TType>(string name, TType defaultValue = default!) => Arguments?.TryGetValue(name, out var value) == true ? (TType)value.Value! : defaultValue;

        /// <inheritdoc cref="Validation.ValidationContext"/>
        public ValidationContext ValidationContext => VisitorContext.ValidationContext;

        /// <inheritdoc cref="NewComplexityConfiguration"/>
        public NewComplexityConfiguration Configuration => VisitorContext.Configuration;

        /// <summary>
        /// Gets the parent field impact context.
        /// </summary>
        public FieldImpactContext? Parent => VisitorContext.FieldAsts.Count == _parentDepth
            ? null
            : new FieldImpactContext
            {
                FieldDefinition = PeekElement(VisitorContext.FieldDefinitions, _parentDepth)!,
                FieldAst = PeekElement(VisitorContext.FieldAsts, _parentDepth)!,
                VisitorContext = VisitorContext,
                _parentDepth = _parentDepth + 1,
            };
    }

    private static T PeekElement<T>(Stack<T> from, int index)
    {
        return index == 0 ? from.Peek() : GetElement(from, index);

        static T GetElement(Stack<T> from, int index)
        {
            if (index >= from.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Stack contains only {from.Count} items");

            using var e = from.GetEnumerator();

            int i = index;
            do
            {
                _ = e.MoveNext();
            }
            while (i-- > 0);

            return e.Current;
        }
    }
}
