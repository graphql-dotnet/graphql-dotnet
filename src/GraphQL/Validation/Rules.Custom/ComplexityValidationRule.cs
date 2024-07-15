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
public class ComplexityValidationRule : ValidationRuleBase
{
    /// <inheritdoc cref="Complexity.ComplexityConfiguration"/>
    protected ComplexityConfiguration ComplexityConfiguration { get; }

    /// <summary>
    /// Initializes an instance with the specified complexity configuration.
    /// </summary>
    public ComplexityValidationRule(ComplexityConfiguration complexityConfiguration)
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
                var complexity = await CalculateComplexityAsync(context).ConfigureAwait(false);
                await ValidateComplexityAsync(context, complexity.TotalComplexity, complexity.MaximumDepth).ConfigureAwait(false);
            }
        return default;
    }


    /// <summary>
    /// Visits the operation specified by <see cref="ValidationContext.Operation"/> to determine its Total Complexity and Maximum Depth.
    /// <para>
    /// Total Complexity is the total complexity of the query, calculated by summing the complexity of each field. Each field's complexity
    /// is multiplied by all of its parent fields' child impact multipliers.
    /// </para>
    /// <para>
    /// Maximum Depth is the maximum depth of the query, calculated by counting the number of nested fields.
    /// </para>
    /// <para>
    /// To determine the complexity of a field, the field's complexity delegate is pulled from the field's metadata (see
    /// <see cref="ComplexityAnalayzerMetadataExtensions.GetComplexityImpactDelegate(FieldType)">GetComplexityImpactFunc</see>).
    /// The complexity delegate is then called with a <see cref="FieldImpactContext"/> containing the field, the parent type, and the visitor context.
    /// The delegate returns a tuple containing the field's complexity and the child impact multiplier. The field's complexity is multiplied by the
    /// parent fields' child impact multipliers to determine the field's total complexity, and this is summed to determine the total complexity of the query.
    /// </para>
    /// <para>
    /// If no complexity delegate is found on a field, the default complexity delegate specified by <see cref="ComplexityConfiguration.DefaultComplexityImpactDelegate"/>
    /// is used. The default implementation computes the field impact as follows: <see cref="ComplexityConfiguration.DefaultScalarImpact"/> for scalar fields and
    /// <see cref="ComplexityConfiguration.DefaultObjectImpact"/> for object fields. The default implementation computes the child impact multiplier as follows:
    /// if the field is a list field, and has an integer 'first' or 'last' argument, the multiplier is the value of the argument. If the field is a list field
    /// and has a 'id' argument supplied, the multiplier is 1. If the field is a list field, and if the parent is not a list field and has a 'first' or 'last'
    /// argument, the multiplier is the value of the argument. Otherwise, the multiplier is <see cref="ComplexityConfiguration.DefaultListImpactMultiplier"/>.
    /// </para>
    /// </summary>
    protected virtual ValueTask<(double TotalComplexity, int MaximumDepth)> CalculateComplexityAsync(ValidationContext context)
        => ComplexityVisitor.RunAsync(context, ComplexityConfiguration);

    /// <summary>
    /// Determines if the computed complexity exceeds the configured threshold.
    /// The default implementation checks if the total complexity exceeds <see cref="ComplexityConfiguration.MaxComplexity"/>
    /// and if the maximum depth exceeds <see cref="ComplexityConfiguration.MaxDepth"/>. If either threshold is exceeded, a
    /// <see cref="ComplexityError"/> is reported. Otherwise, the <see cref="ComplexityConfiguration.ValidateComplexityDelegate"/>
    /// is called.
    /// </summary>
    protected virtual Task ValidateComplexityAsync(ValidationContext context, double totalComplexity, int maxDepth)
    {
        if (totalComplexity > ComplexityConfiguration.MaxComplexity)
            context.ReportError(new ComplexityError(
                $"Query is too complex to execute. Complexity is {totalComplexity}; maximum allowed on this endpoint is {ComplexityConfiguration.MaxComplexity}."));

        if (maxDepth > ComplexityConfiguration.MaxDepth)
            context.ReportError(new ComplexityError(
                $"Query is too nested to execute. Maximum depth is {maxDepth} levels; maximum allowed on this endpoint is {ComplexityConfiguration.MaxDepth}."));

        return !context.HasErrors && ComplexityConfiguration.ValidateComplexityDelegate != null
            ? ComplexityConfiguration.ValidateComplexityDelegate(context, totalComplexity, maxDepth)
            : Task.CompletedTask;
    }

    /// <inheritdoc cref="RunAsync(ValidationContext, ComplexityConfiguration)"/>
    /// <remarks>
    /// Call the <see cref="RunAsync(ValidationContext, ComplexityConfiguration)"/> method to run this visitor.
    /// </remarks>
    internal sealed class ComplexityVisitor : ASTVisitor<VisitorContext>
    {
        private ComplexityVisitor()
        {
        }

        private static readonly ComplexityVisitor _instance = new();

        /// <inheritdoc cref="CalculateComplexityAsync(ValidationContext)"/>
        public static async ValueTask<(double TotalComplexity, int MaximumDepth)> RunAsync(
            ValidationContext validationContext,
            ComplexityConfiguration complexityConfiguration)
        {
            using var context = new VisitorContext(validationContext, complexityConfiguration);

            // visit the operation definition to start the analysis
            // note that any fragments will be visited as they are encountered
            await _instance.VisitOperationDefinitionAsync(validationContext.Operation, context).ConfigureAwait(false);

            return (context.TotalComplexity, context.MaximumDepth);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Called from <see cref="RunAsync(ValidationContext, ComplexityConfiguration)"/> only for the operation selected by the request.
        /// </remarks>
        protected override ValueTask VisitOperationDefinitionAsync(GraphQLOperationDefinition operationDefinition, VisitorContext context)
        {
            // determine the root graph type of the operation
            context.ParentType = operationDefinition.Operation switch
            {
                OperationType.Query => context.Schema.Query,
                OperationType.Mutation => context.Schema.Mutation,
                OperationType.Subscription => context.Schema.Subscription,
                _ => ThrowInvalidOperation(operationDefinition.Operation),
            };

            // ensure the operation type is defined in the schema
            if (context.ParentType == null)
                ThrowOperationNotDefined(operationDefinition.Operation);

            // visit the selection set (ignoring comments, directives, etc)
            return VisitAsync(operationDefinition.SelectionSet.Selections, context);

            [StackTraceHidden, DoesNotReturn]
            static IObjectGraphType ThrowInvalidOperation(OperationType operationType)
                => throw new InvalidOperationException($"Unknown operation type: {operationType}");

            [StackTraceHidden, DoesNotReturn]
            static void ThrowOperationNotDefined(OperationType operationType)
                => throw new InvalidOperationException($"Schema is not configured for operation type: {operationType}");
        }

        protected override async ValueTask VisitFragmentDefinitionAsync(GraphQLFragmentDefinition fragmentDefinition, VisitorContext context)
        {
            // identify the graph type of the fragment and ensure it is defined within the schema
            // don't push the parent type onto the stack, as parent fields are not be affected by the fragment
            var typeName = fragmentDefinition.TypeCondition.Type.Name;
            var oldParentType = context.ParentType;
            context.ParentType = context.Schema.AllTypes[typeName.Value]
                ?? ThrowInvalidType(typeName);

            // visit the selection set (ignoring comments, directives, etc)
            await VisitAsync(fragmentDefinition.SelectionSet.Selections, context).ConfigureAwait(false);
            context.ParentType = oldParentType;

            [StackTraceHidden, DoesNotReturn]
            static IGraphType ThrowInvalidType(GraphQLName typeName)
                => throw new InvalidOperationException($"Type '{typeName.StringValue}' not found in schema.");
        }

        protected override async ValueTask VisitInlineFragmentAsync(GraphQLInlineFragment inlineFragment, VisitorContext context)
        {
            // check @skip and @include directives to determine if the fragment should be included in the analysis
            if (!context.ShouldIncludeNode(inlineFragment))
                return;

            // if the fragment has a type condition, identify the graph type and ensure it is defined within the schema
            // otherwise, it is the same as the parent type
            // note: don't push the parent type onto the stack, as parent fields are not be affected by the fragment
            var oldParentType = context.ParentType;
            if (inlineFragment.TypeCondition != null)
            {
                var typeName = inlineFragment.TypeCondition.Type.Name;
                context.ParentType = context.Schema.AllTypes[typeName.Value]
                    ?? ThrowInvalidType(typeName);
            }

            // visit the selection set (ignoring comments, directives, etc)
            await VisitAsync(inlineFragment.SelectionSet.Selections, context).ConfigureAwait(false);
            context.ParentType = oldParentType;

            [StackTraceHidden, DoesNotReturn]
            static IGraphType ThrowInvalidType(GraphQLName typeName)
                => throw new InvalidOperationException($"Type '{typeName.StringValue}' not found in schema.");
        }

        protected override async ValueTask VisitFragmentSpreadAsync(GraphQLFragmentSpread fragmentSpread, VisitorContext context)
        {
            // check @skip and @include directives to determine if the fragment should be included in the analysis
            if (!context.ShouldIncludeNode(fragmentSpread))
                return;

            // identify the fragment and ensure it is defined within the document
            var fragmentName = fragmentSpread.FragmentName.Name;
            var fragment = context.Document.FindFragmentDefinition(fragmentName.Value)
                ?? ThrowInvalidFragment(fragmentName);

            // check for circular references
            if (context.FragmentsProcessed.Contains(fragment))
                ThrowCircularReference(fragmentName);

            // visit the fragment definition
            context.FragmentsProcessed.Push(fragment);
            await VisitFragmentDefinitionAsync(fragment, context).ConfigureAwait(false);
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
            // check @skip and @include directives to determine if the field should be included in the analysis
            if (!context.ShouldIncludeNode(field))
                return default;

            // identify the field definition and ensure it is defined within the schema
            FieldType fieldType;
            if (field.Name.Value == "__typename")
            {
                // note: context.ParentType may be a union type in this scenario
                fieldType = context.Schema.TypeNameMetaFieldType;
            }
            else if (context.ParentType == context.Schema.Query && field.Name.Value == "__schema")
            {
                fieldType = context.Schema.SchemaMetaFieldType;
            }
            else if (context.ParentType == context.Schema.Query && field.Name.Value == "__type")
            {
                fieldType = context.Schema.TypeMetaFieldType;
            }
            else
            {
                if (context.ParentType is not IComplexGraphType objectGraphType)
                    ThrowNotObjectType(context.ParentType);
                fieldType = objectGraphType.GetField(field.Name.Value)
                    ?? ThrowFieldNotFound(field, objectGraphType);
            }

            // get the complexity impact function for the field
            var complexityImpactFunc = fieldType.GetComplexityImpactDelegate()
                ?? context.Configuration.DefaultComplexityImpactDelegate;

            // calculate the complexity impact of the field
            var complexityImpact = complexityImpactFunc(new FieldImpactContext()
            {
                FieldDefinition = fieldType, // either one of the predefined meta fields (__typename, __schema, or __type) or a field defined on context.ParentType
                FieldAst = field,
                ParentType = context.ParentType!, // parent type is guaranteed to be an object or interface type, or a union type but then the field can only be __typename
                VisitorContext = context,
            });

            // update the total complexity
            context.TotalComplexity += complexityImpact.FieldImpact * context.StandingComplexity;

            // visit the children of the field, if there are any
            // note that even if ChildImpactMultiplier is 0, we still need to visit children to determine the max depth
            return field.SelectionSet != null
                ? VisitChildrenAsync(this, context, field, fieldType, complexityImpact.ChildImpactMultiplier)
                : default;

            static async ValueTask VisitChildrenAsync(ComplexityVisitor visitor, VisitorContext context, GraphQLField field, FieldType fieldType, double multiplier)
            {
                // push the field onto the stack so that complexity calculation delegates can access the parent field if need be
                context.FieldAsts.Push(field);
                context.FieldDefinitions.Push(fieldType);
                context.ParentTypes.Push(context.ParentType!);
                // identify the parent type of the field
                context.ParentType = fieldType.ResolvedType!.GetNamedType();
                // increment the depth (indicating the depth at this field)
                context.TotalDepth++;
                // update the maximum depth
                context.MaximumDepth = Math.Max(context.MaximumDepth, context.TotalDepth);
                // update the standing complexity
                var oldMultiplier = context.StandingComplexity;
                context.StandingComplexity *= multiplier;
                // visit the selection set (ignoring comments, directives, etc)
                await visitor.VisitAsync(field.SelectionSet!.Selections, context).ConfigureAwait(false);
                // restore the context
                context.StandingComplexity = oldMultiplier;
                context.TotalDepth--;
                context.ParentType = context.ParentTypes.Pop();
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
        private static Stack<IGraphType>? _sharedParentTypes;
        private static Stack<GraphQLFragmentDefinition>? _sharedFragmentsProcessed;
        public VisitorContext(ValidationContext validationContext, ComplexityConfiguration complexityConfiguration)
        {
            ValidationContext = validationContext;
            Configuration = complexityConfiguration;
        }

        /// <summary>This stack is used to provide <see cref="FieldImpactContext.Parent"/> functionality, tracking parent field definitions.</summary>
        public readonly Stack<GraphQLField> FieldAsts = Interlocked.Exchange(ref _sharedFieldAsts, null) ?? new();
        /// <summary>This stack is used to provide <see cref="FieldImpactContext.Parent"/> functionality, tracking parent field AST nodes.</summary>
        public readonly Stack<FieldType> FieldDefinitions = Interlocked.Exchange(ref _sharedFieldDefinitions, null) ?? new();
        /// <summary>This stack is used to provide <see cref="FieldImpactContext.Parent"/> functionality, tracking parent field parent graph types.</summary>
        public readonly Stack<IGraphType> ParentTypes = Interlocked.Exchange(ref _sharedParentTypes, null) ?? new();
        /// <summary>This stack is used to detect circular references in fragments.</summary>
        public readonly Stack<GraphQLFragmentDefinition> FragmentsProcessed = Interlocked.Exchange(ref _sharedFragmentsProcessed, null) ?? new();
        /// <inheritdoc cref="ValidationContext"/>
        public readonly ValidationContext ValidationContext;
        /// <inheritdoc cref="ComplexityConfiguration"/>
        public readonly ComplexityConfiguration Configuration;
        /// <inheritdoc cref="FieldImpactContext.ParentType"/>
        public IGraphType? ParentType;
        /// <summary>The total complexity calculated so far.</summary>
        public double TotalComplexity;
        /// <summary>The standing complexity of the current field. Multiply any field impact complexity by this value before adding to <see cref="TotalComplexity"/>.</summary>
        public double StandingComplexity = 1;
        /// <summary>The depth for this parent type. Increment only when tracking a field's child selection set. Do not increment when processing a fragment selection set.</summary>
        public int TotalDepth = 1;
        /// <summary>The maximum depth of the query found so far.</summary>
        public int MaximumDepth = 1;

        /// <inheritdoc cref="ValidationContext.ArgumentValues"/>
        public IDictionary<GraphQLField, IDictionary<string, ArgumentValue>>? Arguments => ValidationContext.ArgumentValues;
        /// <inheritdoc cref="ValidationContext.Schema"/>
        public ISchema Schema => ValidationContext.Schema;
        /// <inheritdoc cref="ValidationContext.Document"/>
        public GraphQLDocument Document => ValidationContext.Document;
        /// <inheritdoc cref="ValidationContext.Operation"/>
        public GraphQLOperationDefinition Operation => ValidationContext.Operation;
        /// <inheritdoc cref="ValidationContext.ShouldIncludeNode(ASTNode)"/>
        public bool ShouldIncludeNode(ASTNode node) => ValidationContext.ShouldIncludeNode(node);
        /// <inheritdoc cref="ValidationContext.CancellationToken"/>
        /// <remarks>n/a since this visitor does not call any asynchronous methods</remarks>
        public CancellationToken CancellationToken => ValidationContext.CancellationToken;

        /// <summary>Returns the stacks to the static instances so they can be reused.</summary>
        public void Dispose()
        {
            // The stacks should be empty, so we can reuse them. If they are not empty, it is because an unexpected exception was thrown.
            if (FieldAsts.Count > 0 || FieldDefinitions.Count > 0 || FragmentsProcessed.Count > 0 || ParentTypes.Count > 0)
                return;
            Interlocked.Exchange(ref _sharedFieldAsts, FieldAsts);
            Interlocked.Exchange(ref _sharedFieldDefinitions, FieldDefinitions);
            Interlocked.Exchange(ref _sharedFragmentsProcessed, FragmentsProcessed);
            Interlocked.Exchange(ref _sharedParentTypes, ParentTypes);
        }
    }
}
