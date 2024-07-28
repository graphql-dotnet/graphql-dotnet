using GraphQL.Execution;
using GraphQL.Types;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation.Complexity;

/// <summary>
/// Context for <see cref="ComplexityVisitor"/>.
/// </summary>
internal sealed class ComplexityVisitorContext : IASTVisitorContext, IDisposable
{
    private static Stack<GraphQLField>? _sharedFieldAsts;
    private static Stack<FieldType>? _sharedFieldDefinitions;
    private static Stack<IGraphType>? _sharedParentTypes;
    private static Stack<GraphQLFragmentDefinition>? _sharedFragmentsProcessed;
    public ComplexityVisitorContext(ValidationContext validationContext, ComplexityOptions complexityConfiguration)
    {
        ValidationContext = validationContext;
        Configuration = complexityConfiguration;
    }

    /// <inheritdoc cref="Validation.ValidationContext"/>
    public readonly ValidationContext ValidationContext;
    /// <inheritdoc cref="ComplexityOptions"/>
    public readonly ComplexityOptions Configuration;

    // fragment tracking stack for circular reference detection

    /// <summary>This stack is used to detect circular references in fragments.</summary>
    public readonly Stack<GraphQLFragmentDefinition> FragmentsProcessed = Interlocked.Exchange(ref _sharedFragmentsProcessed, null) ?? new();

    // parent tracking stacks

    /// <summary>This stack is used to provide <see cref="FieldImpactContext.Parent"/> functionality, tracking parent field definitions.</summary>
    public readonly Stack<GraphQLField> FieldAsts = Interlocked.Exchange(ref _sharedFieldAsts, null) ?? new();
    /// <summary>This stack is used to provide <see cref="FieldImpactContext.Parent"/> functionality, tracking parent field AST nodes.</summary>
    public readonly Stack<FieldType> FieldDefinitions = Interlocked.Exchange(ref _sharedFieldDefinitions, null) ?? new();
    /// <summary>This stack is used to provide <see cref="FieldImpactContext.Parent"/> functionality, tracking parent field parent graph types.</summary>
    public readonly Stack<IGraphType> ParentTypes = Interlocked.Exchange(ref _sharedParentTypes, null) ?? new();

    // current graph type tracking and complexity tracking

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

    // helper properties (maps to ValidationContext properties)

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
