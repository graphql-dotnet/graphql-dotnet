using GraphQL.Types;

namespace GraphQL.Execution;

/// <summary>
/// Represents information about directive that has been provided in the GraphQL query request.
/// </summary>
public class DirectiveInfo
{
    private readonly ISchema _schema;

    /// <summary>
    /// Creates an instance of <see cref="DirectiveInfo"/> with the specified
    /// directive definition, directive arguments, and schema.
    /// </summary>
    public DirectiveInfo(Directive directive, IDictionary<string, ArgumentValue> arguments, ISchema schema)
    {
        Directive = directive;
        Arguments = arguments;
        _schema = schema;
    }

    /// <summary>
    /// Directive definition.
    /// </summary>
    public Directive Directive { get; }

    /// <summary>
    /// Dictionary of directive arguments.
    /// </summary>
    public IDictionary<string, ArgumentValue> Arguments { get; }

    /// <summary>
    /// Returns the value of the specified directive argument, or <paramref name="defaultValue"/> when
    /// unspecified. Variable default values take precedence over the <paramref name="defaultValue"/> parameter.
    /// </summary>
    public TType GetArgument<TType>(string name, TType defaultValue = default!)
    {
        bool exists = TryGetArgument(typeof(TType), name, out object? result);
        return exists
            ? (TType)result!
            : defaultValue;
    }

    /// <inheritdoc cref="GetArgument{TType}(string, TType)"/>
    public object? GetArgument(Type argumentType, string name, object? defaultValue = null)
    {
        bool exists = TryGetArgument(argumentType, name, out object? result);
        return exists
            ? result
            : defaultValue;
    }

    // initially copy-pasted from ResolveFieldContextExtensions.TryGetArgument and then modified
    private bool TryGetArgument(Type argumentType, string name, out object? result)
    {
        if (Arguments == null || !Arguments.TryGetValue(name, out var arg))
        {
            result = null;
            return false;
        }

        var resolvedType = Directive.Arguments?.Find(name)?.ResolvedType
            ?? throw new InvalidOperationException($"Could not obtain graph type instance for argument '{name}'");

        if (arg.Value is IDictionary<string, object?> inputObject)
        {
            if (argumentType == typeof(object))
            {
                result = arg.Value;
                return true;
            }

            result = inputObject.ToObject(argumentType, resolvedType, _schema.ValueConverter);
            return true;
        }

        result = arg.Value.GetPropertyValue(argumentType, resolvedType, _schema.ValueConverter);
        return true;
    }
}
