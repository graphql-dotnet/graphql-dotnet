using GraphQL.Language.AST;
using GraphQL.Validation;
using GraphQL.Validation.Rules;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Sample.ValidationRules;

/// <summary>
/// This validation rule limits the length of string input values to a maximum of 500 characters.
/// It demonstrates how to validate input arguments after they have been parsed.
/// </summary>
public class InputFieldsOfCorrectLengthValidationRule : ValidationRuleBase, INodeVisitor
{
    private const int MAX_LENGTH = 500;

    /// <summary>
    /// Returns a node visitor that runs after argument parsing.
    /// This allows us to access the parsed argument values.
    /// </summary>
    public override ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context)
        // Only add this visitor if there are parsed argument values available
        => context.ArgumentValues != null ? new(this) : default;

    /// <summary>
    /// Called when entering a field node in the AST.
    /// </summary>
    public ValueTask INodeVisitor.EnterAsync(ASTNode node, ValidationContext context)
    {
        if (node is not GraphQLField fieldNode)
            return default;

        // Get the field definition from the type info
        var fieldDef = context.TypeInfo.GetFieldDef();
        if (fieldDef == null)
            return default;

        // Try to get the argument values for this field
        if (!context.ArgumentValues!.TryGetValue(fieldNode, out var args) || args == null)
            return default;

        // Check each argument value
        foreach (var arg in args)
        {
            if (arg.Value.Value is string stringValue && stringValue.Length > MAX_LENGTH)
            {
                context.ReportError(new ValidationError(
                    context.Document.Source,
                    null,
                    $"Argument '{arg.Key.Name}' exceeds maximum length of {MAX_LENGTH} characters",
                    fieldNode));
            }
        }

        return default;
    }

    /// <summary>
    /// Called when leaving a field node in the AST.
    /// </summary>
    public ValueTask INodeVisitor.LeaveAsync(ASTNode node, ValidationContext context) => default;
}
