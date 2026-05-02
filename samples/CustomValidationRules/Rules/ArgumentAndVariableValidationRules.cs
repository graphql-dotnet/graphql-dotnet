using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace CustomValidationRules.Rules;

/// <summary>
/// A validation rule that demonstrates the use of <see cref="FieldType.ValidateArguments"/>
/// through a custom validation rule. While <see cref="FieldType.ValidateArguments"/> is
/// typically set via the <c>[ValidateArguments]</c> attribute or the field builder,
/// this sample shows how to build an equivalent rule manually using <c>GetPostNodeVisitorAsync</c>.
/// <para>
/// The post-node visitor runs after arguments have been parsed, so
/// <see cref="ValidationContext.ArgumentValues"/> is available with the parsed argument values.
/// </para>
/// </summary>
public class ArgumentValidationRule : ValidationRuleBase
{
    /// <summary>
    /// Returns a post-node visitor that validates field arguments after they have been parsed.
    /// </summary>
    public override ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context)
        => new(_visitor);

    private static readonly MatchingNodeVisitor<GraphQLField> _visitor = new(
        enter: (fieldNode, context) =>
        {
            var fieldDef = context.TypeInfo.GetFieldDef();
            if (fieldDef == null)
                return;

            // Access parsed argument values from the post-validation phase
            if (context.ArgumentValues != null &&
                context.ArgumentValues.TryGetValue(fieldNode, out var args))
            {
                // Example: validate that 'limit' argument is within acceptable range
                if (args.TryGetValue("limit", out var limitArg))
                {
                    if (limitArg.Value is int limit && limit > 1000)
                    {
                        context.ReportError(new ValidationError(
                            context.Document.Source,
                            "ARGUMENT_OUT_OF_RANGE",
                            $"Argument 'limit' on field '{fieldDef.Name}' must not exceed 1000. Got: {limit}",
                            fieldNode));
                    }
                }
            }
        });
}

/// <summary>
/// A validation rule demonstrating the <see cref="IVariableVisitor"/> pattern.
/// Variable visitors are called during variable parsing, allowing custom validation
/// of variable values as they are being parsed.
/// <para>
/// This is useful for cross-field validation within input objects, or for
/// applying custom parsing/transformation to variables before they reach the resolver.
/// </para>
/// </summary>
public class CustomVariableVisitorRule : ValidationRuleBase
{
    public override ValueTask<IVariableVisitor?> GetVariableVisitorAsync(ValidationContext context)
        => new(CustomVariableVisitor.Instance);

    private class CustomVariableVisitor : BaseVariableVisitor
    {
        public static readonly CustomVariableVisitor Instance = new();

        public override ValueTask VisitFieldAsync(
            ValidationContext context,
            GraphQLVariableDefinition variable,
            VariableName variableName,
            IInputObjectGraphType type,
            FieldType field,
            object? variableValue,
            object? parsedValue)
        {
            // Example: validate that string fields marked with "noWhitespace" metadata
            // do not contain whitespace characters
            if (field.GetMetadata<bool>("noWhitespace") && parsedValue is string str)
            {
                if (str.Any(char.IsWhiteSpace))
                {
                    context.ReportError(new ValidationError(
                        context.Document.Source,
                        "NO_WHITESPACE",
                        $"Field '{field.Name}' on variable '{variableName}' must not contain whitespace.",
                        variable));
                }
            }

            return default;
        }
    }
}

/// <summary>
/// Base class for variable visitors with no-op implementations of all methods.
/// Inherit from this class and override only the methods you need.
/// </summary>
public abstract class BaseVariableVisitor : IVariableVisitor
{
    /// <inheritdoc/>
    public virtual ValueTask VisitScalarAsync(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, ScalarGraphType type, object? variableValue, object? parsedValue)
        => default;

    /// <inheritdoc/>
    public virtual ValueTask VisitListAsync(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, ListGraphType type, object? variableValue, IList<object?>? parsedValue)
        => default;

    /// <inheritdoc/>
    public virtual ValueTask VisitObjectAsync(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, IInputObjectGraphType type, object? variableValue, object? parsedValue)
        => default;

    /// <inheritdoc/>
    public virtual ValueTask VisitFieldAsync(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, IInputObjectGraphType type, FieldType field, object? variableValue, object? parsedValue)
        => default;
}
