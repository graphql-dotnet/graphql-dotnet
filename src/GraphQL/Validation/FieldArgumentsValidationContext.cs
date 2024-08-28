using GraphQL.Execution;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Validation;

/// <summary>
/// Represents the context for validating field arguments.
/// </summary>
public struct FieldArgumentsValidationContext
{
    /// <inheritdoc cref="IResolveFieldContext.FieldAst"/>
    public GraphQLField FieldAst { get; set; }

    /// <inheritdoc cref="IResolveFieldContext.FieldDefinition"/>
    public FieldType FieldDefinition { get; set; }

    private IGraphType? _parentType;
    /// <inheritdoc cref="IResolveFieldContext.ParentType"/>
    public IGraphType ParentType => _parentType ??= (ValidationContext.TypeInfo.GetLastType() ?? throw new InvalidOperationException("Unable to retrieve the parent type for this field."));

    /// <inheritdoc cref="Validation.ValidationContext"/>
    public ValidationContext ValidationContext { get; set; }

    private IDictionary<string, ArgumentValue>? _arguments;
    private bool _argumentsSet;
    /// <inheritdoc cref="IResolveFieldContext.Arguments"/>
    public IDictionary<string, ArgumentValue>? Arguments
    {
        get
        {
            if (_argumentsSet)
                return _arguments;
            _argumentsSet = true;
            return _arguments = ValidationContext.ArgumentValues?.TryGetValue(FieldAst, out var args) == true ? args : null;
        }
        set
        {
            ValidationContext.ArgumentValues ??= new();
            if (value != null)
                ValidationContext.ArgumentValues[FieldAst] = value;
            else
                ValidationContext.ArgumentValues.Remove(FieldAst);
            _arguments = value;
            _argumentsSet = true;
        }
    }

    /// <inheritdoc cref="IResolveFieldContext.Directives"/>
    public IDictionary<string, DirectiveInfo>? Directives => ValidationContext.DirectiveValues?.TryGetValue(FieldAst, out var dirs) == true ? dirs : null;

    /// <inheritdoc cref="IResolveFieldContext.RequestServices"/>
    public IServiceProvider? RequestServices => ValidationContext.RequestServices;

    /// <inheritdoc cref="IResolveFieldContext.CancellationToken"/>
    public CancellationToken CancellationToken => ValidationContext.CancellationToken;

    /// <summary>
    /// Gets the argument specified by name.
    /// </summary>
    public T GetArgument<T>(string name, T defaultValue = default!)
    {
        if (Arguments?.TryGetValue(name, out var arg) == true)
            return (T)arg.Value! ?? defaultValue;

        return defaultValue;
    }

    /// <summary>
    /// Sets the argument specified by name.
    /// </summary>
    public void SetArgument(string name, object? value)
    {
        (Arguments ??= new Dictionary<string, ArgumentValue>())[name] = new ArgumentValue(value, ArgumentSource.Literal);
    }
}
