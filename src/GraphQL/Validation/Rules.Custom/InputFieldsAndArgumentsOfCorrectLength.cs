using GraphQL.Types;
using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks minimum and maximum length of provided values for input fields and
    /// arguments that marked with <see cref="LengthDirective"/> directive. Doesn't check default values.
    /// <br/><br/>
    /// This is not a standard validation rule that is not in the official specification. Note that this
    /// rule will be required to run on cached queries also since it works with request variables, so
    /// <see cref="ExecutionOptions.CachedDocumentValidationRules">ExecutionOptions.CachedDocumentValidationRules</see>
    /// needs to be set as well (if using caching).
    /// </summary>
    public class InputFieldsAndArgumentsOfCorrectLength : IValidationRule, IVariableVisitorProvider
    {
        private sealed class FieldVisitor : BaseVariableVisitor
        {
            public static readonly FieldVisitor Instance = new();

            public override void VisitField(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, IInputObjectGraphType type, FieldType field, object? variableValue, object? parsedValue)
            {
                var lengthDirective = field.FindAppliedDirective("length");
                if (lengthDirective == null)
                    return;

                var min = lengthDirective.FindArgument("min")?.Value;
                var max = lengthDirective.FindArgument("max")?.Value;

                if (parsedValue == null)
                {
                    if (min != null)
                        context.ReportError(new InputFieldsAndArgumentsOfCorrectLengthError(context, variable, variableName, null, (int?)min, (int?)max));
                }
                else if (parsedValue is string str)
                {
                    if (min != null && str.Length < (int)min || max != null && str.Length > (int)max)
                        context.ReportError(new InputFieldsAndArgumentsOfCorrectLengthError(context, variable, variableName, str.Length, (int?)min, (int?)max));
                }
            }
        }

        /// <inheritdoc/>
        public IVariableVisitor GetVisitor(ValidationContext _) => FieldVisitor.Instance;

        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly InputFieldsAndArgumentsOfCorrectLength Instance = new();

        /// <inheritdoc/>
        /// <exception cref="InputFieldsAndArgumentsOfCorrectLengthError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<GraphQLArgument>((arg, context) => CheckLength(arg, arg.Value, context.TypeInfo.GetArgument(), context)),
            new MatchingNodeVisitor<GraphQLObjectField>((field, context) =>
            {
                if (context.TypeInfo.GetInputType(1) is IInputObjectGraphType input)
                    CheckLength(field, field.Value, input.GetField(field.Name), context);
            })
        );

        private static void CheckLength(ASTNode node, GraphQLValue value, IProvideMetadata? provider, ValidationContext context)
        {
            var lengthDirective = provider?.FindAppliedDirective("length");
            if (lengthDirective == null)
                return;

            var min = lengthDirective.FindArgument("min")?.Value;
            var max = lengthDirective.FindArgument("max")?.Value;

            if (value is GraphQLNullValue)
            {
                if (min != null)
                    context.ReportError(new InputFieldsAndArgumentsOfCorrectLengthError(context, node, null, (int?)min, (int?)max));
            }
            else if (value is GraphQLStringValue strLiteral)
            {
                CheckStringLength(strLiteral.Value.Length);
            }
            else if (value is GraphQLVariable vRef && context.Variables != null && context.Variables.TryGetValue(vRef.Name.StringValue, out object? val)) //ISSUE:allocation
            {
                if (val is string strVariable)
                    CheckStringLength(strVariable.Length);
                else if (val is null && min != null)
                    context.ReportError(new InputFieldsAndArgumentsOfCorrectLengthError(context, node, null, (int?)min, (int?)max));
            }

            void CheckStringLength(int length)
            {
                if (min != null && length < (int)min || max != null && length > (int)max)
                    context.ReportError(new InputFieldsAndArgumentsOfCorrectLengthError(context, node, length, (int?)min, (int?)max));
            }
        }
    }
}
