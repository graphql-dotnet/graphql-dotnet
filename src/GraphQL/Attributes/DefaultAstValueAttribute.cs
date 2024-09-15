using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL;

/// <summary>
/// Specifies the default value for an argument or a field of an input object.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class DefaultAstValueAttribute : GraphQLAttribute
{
    private readonly GraphQLValue _value;
    private static readonly ParserOptions _options = new() { Ignore = IgnoreOptions.All };

    /// <inheritdoc cref="DefaultAstValueAttribute"/>
    public DefaultAstValueAttribute(string astValue)
    {
        _value = Parser.Parse<GraphQLValue>(astValue, _options);
    }

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        if (isInputType)
        {
            fieldType.DefaultValue = _value;
        }
    }

    /// <inheritdoc/>
    public override void Modify(QueryArgument queryArgument)
    {
        queryArgument.DefaultValue = _value;
    }

    /// <inheritdoc/>
    public override void Modify(FieldConfig field)
    {
        field.DefaultValue = _value;
    }
}
