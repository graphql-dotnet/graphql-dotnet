using GraphQL.Execution;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Validation.Complexity;

/// <summary>
/// Provides contextual information about the field being analyzed.
/// </summary>
public struct FieldImpactContext
{
    /// <summary>
    /// The <see cref="FieldType"/> definition defined within the graph type specified by <see cref="ParentType"/>, or
    /// one of the predefined meta fields (__typename, __schema, or __type), which are defined by
    /// <see cref="ISchema.TypeNameMetaFieldType"/>, <see cref="ISchema.SchemaMetaFieldType"/>, and <see cref="ISchema.TypeMetaFieldType"/>.
    /// </summary>
    public FieldType FieldDefinition { get; set; }

    /// <inheritdoc cref="IResolveFieldContext.FieldAst"/>
    public GraphQLField FieldAst { get; set; }

    /// <summary>
    /// The field's parent graph type. For meta fields, this defines the parent graph type where the meta field was requested.
    /// For __typename, this type may be a <see cref="UnionGraphType"/>; otherwise it is an object or interface graph type and
    /// may be coerced to <see cref="IComplexGraphType"/>.
    /// </summary>
    public IGraphType ParentType { get; set; }

    internal ComplexityVisitorContext VisitorContext;
    private int _parentDepth;

    /// <inheritdoc cref="IResolveFieldContext.Arguments"/>
    public IDictionary<string, ArgumentValue>? Arguments
    {
        get => field ??= (VisitorContext.Arguments?.TryGetValue(FieldAst, out var args) == true ? args : null);
        private set;
    }

    /// <inheritdoc cref="ResolveFieldContextExtensions.GetArgument{TType}(IResolveFieldContext, string, TType)"/>
    /// <remarks>Does not coerce the argument name via <see cref="ISchema.NameConverter"/>.</remarks>
    public TType GetArgument<TType>(string name, TType defaultValue = default!) => Arguments?.TryGetValue(name, out var value) == true ? (TType)value.Value! : defaultValue;

    /// <inheritdoc cref="Validation.ValidationContext"/>
    public ValidationContext ValidationContext => VisitorContext.ValidationContext;

    /// <inheritdoc cref="Complexity.ComplexityOptions"/>
    public ComplexityOptions Configuration => VisitorContext.Configuration;

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
