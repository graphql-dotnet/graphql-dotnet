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

    /// <inheritdoc cref="IResolveFieldContext.ParentType"/>
    public IGraphType? ParentType => field ??= ValidationContext.TypeInfo.GetLastType();

    /// <inheritdoc cref="Validation.ValidationContext"/>
    public ValidationContext ValidationContext { get; set; }

    private bool _argumentsSet;
    /// <inheritdoc cref="IResolveFieldContext.Arguments"/>
    public IDictionary<string, ArgumentValue>? Arguments
    {
        get
        {
            if (_argumentsSet)
                return field;
            _argumentsSet = true;
            return field = ValidationContext.ArgumentValues?.TryGetValue(FieldAst, out var args) == true ? args : null;
        }
        set
        {
            ValidationContext.ArgumentValues ??= [];
            if (value != null)
                ValidationContext.ArgumentValues.TryAdd(FieldAst, value);
            else
                ValidationContext.ArgumentValues.TryRemove(FieldAst, out _);
            field = value;
            _argumentsSet = true;
        }
    }

    /// <inheritdoc cref="IResolveFieldContext.Directives"/>
    public IDictionary<string, DirectiveInfo>? Directives => ValidationContext.DirectiveValues?.TryGetValue(FieldAst, out var dirs) == true ? dirs : null;

    /// <inheritdoc cref="IResolveFieldContext.RequestServices"/>
    public readonly IServiceProvider? RequestServices => ValidationContext.RequestServices;

    /// <inheritdoc cref="IResolveFieldContext.CancellationToken"/>
    public readonly CancellationToken CancellationToken => ValidationContext.CancellationToken;

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

    /// <inheritdoc cref="ValidationContext.ReportError(ValidationError)"/>
    public readonly void ReportError(ValidationError error)
    {
        error.AddNode(ValidationContext.Document.Source, FieldAst);
        ValidationContext.ReportError(error);
    }
}
