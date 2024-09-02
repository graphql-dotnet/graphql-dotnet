using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// Validates that field arguments are valid.
/// </summary>
public sealed class FieldArgumentsAreValidRule : ValidationRuleBase, INodeVisitor
{
    /// <summary>
    /// The key for the metadata that indicates if the schema has field argument validation visitors.
    /// </summary>
    public const string HAS_FIELD_ARGUMENT_VALIDATION_KEY = "__GraphQL_Has_Field_Argument_Validation__";

    private FieldArgumentsAreValidRule()
    {
    }

    /// <summary>
    /// Returns a new instance of the rule.
    /// </summary>
    public static FieldArgumentsAreValidRule Instance { get; } = new();

    /// <inheritdoc/>
    public override ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context)
        // only execute this validation rule when there's at least one field with argument validation
        // since this triggers the TypeInfo visitor to run during this phase, whereas normally there are
        // no post-node visitors
        => context.Schema.GetMetadata<bool>(HAS_FIELD_ARGUMENT_VALIDATION_KEY) ? new(this) : default;

    ValueTask INodeVisitor.EnterAsync(ASTNode node, ValidationContext context)
    {
        // only execute for fields of non-input graph types
        if (node is not GraphQLField fieldNode || context.TypeInfo.GetLastType() is IInputObjectGraphType)
            return default;

        // only execute when the field has a validation method
        var field = context.TypeInfo.GetFieldDef();
        if (field?.ValidateArguments == null)
            return default;

        var ctx = new FieldArgumentsValidationContext
        {
            FieldAst = fieldNode,
            FieldDefinition = field,
            ValidationContext = context,
        };

        return Validate(field.ValidateArguments, ctx);

        static async ValueTask Validate(Func<FieldArgumentsValidationContext, ValueTask> func, FieldArgumentsValidationContext ctx)
        {
            try
            {
                await func(ctx).ConfigureAwait(false);
            }
            catch (ValidationError ex)
            {
                ex.AddNode(ctx.ValidationContext.Document.Source, ctx.FieldAst);
                ctx.ValidationContext.ReportError(ex);
            }
            // note: ValidationContext can only contain ValidationErrors, not ExecutionErrors
            catch (ExecutionError ex)
            {
                ex.AddLocation(ctx.FieldAst, ctx.ValidationContext.Document);
                throw;
            }
            // note: do not catch any other exceptions; let them bubble up to the DocumentExecuter to be handled by the unhandled exception delegate
        }
    }

    ValueTask INodeVisitor.LeaveAsync(ASTNode node, ValidationContext context) => default;
}
