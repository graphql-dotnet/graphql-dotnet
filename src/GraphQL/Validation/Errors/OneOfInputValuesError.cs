using GraphQLParser.AST;

namespace GraphQL.Validation.Errors;

/// <summary>
/// If the input object literal or unordered map does not contain exactly one
/// entry an error must be thrown.
/// </summary>
[Serializable]
public class OneOfInputValuesError : ValidationError
{
    internal const string NUMBER = "3.10";

    internal const string MULTIPLE_VALUES = "Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.";

    /// <summary>
    /// Initializes a new instance with the specified properties.
    /// </summary>
    public OneOfInputValuesError(ValidationContext context, GraphQLValue node)
        : base(context.Document.Source, NUMBER, MULTIPLE_VALUES, node)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified properties.
    /// </summary>
    public OneOfInputValuesError(ValidationContext context, GraphQLObjectField node)
        : base(context.Document.Source, NUMBER, MULTIPLE_VALUES, node, node.Value)
    {
    }
}
