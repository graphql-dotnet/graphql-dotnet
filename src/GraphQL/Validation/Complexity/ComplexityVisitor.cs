using System.Diagnostics;
using GraphQL.Types;
using GraphQL.Validation.Rules.Custom;
using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation.Complexity;

/// <inheritdoc cref="RunAsync(ValidationContext, ComplexityOptions)"/>
/// <remarks>
/// Call the <see cref="RunAsync(ValidationContext, ComplexityOptions)"/> method to run this visitor.
/// <para>
/// This class contains a number of query validation checks. All of these checks should be handled
/// by the default set of validation rules, and if detected, complexity analysis should be skipped.
/// As such, no exceptions should be thrown directly by this class. Checks still exist within this
/// class to prevent stack overflow exceptions and other issues that may arise from invalid queries.
/// Note that since users may set their own calculation logic, it is still possible that exceptions
/// may bubble from there. The <see cref="DocumentExecuter"/> should wrap unidentified exceptions in
/// an <see cref="ExecutionError"/> via the unhandled exception delegate and return them to the client.
/// </para>
/// </remarks>
internal sealed class ComplexityVisitor : ASTVisitor<ComplexityVisitorContext>
{
    private ComplexityVisitor()
    {
    }

    private static readonly ComplexityVisitor _instance = new();

    /// <inheritdoc cref="ComplexityValidationRule.CalculateComplexityAsync(ValidationContext)"/>
    public static async ValueTask<(double TotalComplexity, int MaximumDepth)> RunAsync(
        ValidationContext validationContext,
        ComplexityOptions complexityConfiguration)
    {
        // disposing the visitor context will stash the various empty Stack instances for later re-use
        using var context = new ComplexityVisitorContext(validationContext, complexityConfiguration);

        // visit the operation definition to start the analysis
        // note that any fragments will be visited as they are encountered
        await _instance.VisitOperationDefinitionAsync(validationContext.Operation, context).ConfigureAwait(false);

        return (context.TotalComplexity, context.MaximumDepth);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Called from <see cref="RunAsync(ValidationContext, ComplexityOptions)"/> only for the operation selected by the request.
    /// </remarks>
    protected override ValueTask VisitOperationDefinitionAsync(GraphQLOperationDefinition operationDefinition, ComplexityVisitorContext context)
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

    protected override async ValueTask VisitFragmentDefinitionAsync(GraphQLFragmentDefinition fragmentDefinition, ComplexityVisitorContext context)
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

    protected override async ValueTask VisitInlineFragmentAsync(GraphQLInlineFragment inlineFragment, ComplexityVisitorContext context)
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

    protected override async ValueTask VisitFragmentSpreadAsync(GraphQLFragmentSpread fragmentSpread, ComplexityVisitorContext context)
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

    protected override ValueTask VisitFieldAsync(GraphQLField field, ComplexityVisitorContext context)
    {
        // check @skip and @include directives to determine if the field should be included in the analysis
        if (!context.ShouldIncludeNode(field))
            return default;

        // identify the field definition and ensure it is defined within the schema
        FieldType fieldType;
        if (field.Name.Value == "__typename")
            // note: context.ParentType may be a union type in this scenario
            fieldType = context.Schema.TypeNameMetaFieldType;
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

        static async ValueTask VisitChildrenAsync(ComplexityVisitor visitor, ComplexityVisitorContext context, GraphQLField field, FieldType fieldType, double multiplier)
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
            => throw new InvalidOperationException($"Type '{type?.Name}' is not an object or interface type.");

        [StackTraceHidden, DoesNotReturn]
        static FieldType ThrowFieldNotFound(GraphQLField field, IComplexGraphType type)
            => throw new InvalidOperationException($"Field '{field.Name.StringValue}' not found in type '{type.Name}'.");
    }
}
