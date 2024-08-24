using System.Text.RegularExpressions;
using GraphQLParser.AST;

namespace GraphQL.Types;

/// <summary>
/// Regular expression graph type.
/// </summary>
public class RegexGraphType : ScalarGraphType
{
    /// <inheritdoc/>
    public override object? ParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLStringValue stringValue => new Regex((string)stringValue.Value),
        _ => ThrowLiteralConversionError(value)
    };

    /// <inheritdoc/>
    public override object? ParseValue(object? value) => value switch
    {
        string s => new Regex(s),
        Regex r => r,
        _ => ThrowValueConversionError(value)
    };

    /// <inheritdoc/>
    public override GraphQLValue ToAST(object? value) => value switch
    {
        Regex r => new GraphQLStringValue(StripChars(r)),
        string s => new GraphQLStringValue(s),
        _ => ThrowASTConversionError(value)
    };

    private static string StripChars(Regex r)
    {
        var s = r.ToString();
        if (s.StartsWith("^") && s.EndsWith("$"))
        {
            s = s.Substring(1);
            s = s.Substring(0, s.Length - 1);
        }
        else
            throw new InvalidOperationException("Regex must start with ^ and end with $");
        return s;
    }
}
