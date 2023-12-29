using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Defines extension methods for <see cref="QueryArgument"/>
/// </summary>
public static class QueryArgumentExtensions
{
    /// <summary>
    /// Sets the input coercion for the argument, replacing any existing coercion function.
    /// Runs before any validation defined via <see cref="QueryArgument.Validator"/>.
    /// Null values are not passed to this function.
    /// </summary>
    public static QueryArgument ParseValue(this QueryArgument argument, Func<object, object> parseValue)
    {
        argument.Parser = parseValue;
        return argument;
    }

    /// <summary>
    /// Adds validation to the argument, appending it to any existing validation function.
    /// Runs after any coercion defined within <see cref="QueryArgument.Parser"/>.
    /// Null values are not passed to this function.
    /// </summary>
    public static QueryArgument Validate(this QueryArgument argument, Action<object> validator)
    {
        argument.Validator = argument.Validator == QueryArgument.DefaultValidator
            ? validator
            : argument.Validator + validator;

        return argument;
    }
}
