#nullable enable

using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation.Errors;

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
            public static readonly FieldVisitor Instance = new FieldVisitor();

            public override void VisitField(ValidationContext context, VariableDefinition variable, VariableName variableName, IInputObjectGraphType type, FieldType field, object? variableValue, object? parsedValue)
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
        public static readonly InputFieldsAndArgumentsOfCorrectLength Instance = new InputFieldsAndArgumentsOfCorrectLength();

        /// <inheritdoc/>
        /// <exception cref="InputFieldsAndArgumentsOfCorrectLengthError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _nodeVisitor;

        private static readonly Task<INodeVisitor> _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<Argument>((arg, context) => CheckLength(arg, context.TypeInfo.GetArgument(), context)),
            new MatchingNodeVisitor<ObjectField>((field, context) =>
            {
                if (context.TypeInfo.GetInputType(1) is InputObjectGraphType input)
                    CheckLength(field, input.GetField(field.Name), context);
            })
        ).ToTask();

        private static void CheckLength(IHaveValue node, IProvideMetadata? provider, ValidationContext context)
        {
            var lengthDirective = provider?.FindAppliedDirective("length");
            if (lengthDirective == null)
                return;

            var min = lengthDirective.FindArgument("min")?.Value;
            var max = lengthDirective.FindArgument("max")?.Value;

            if (node.Value is NullValue)
            {
                if (min != null)
                    context.ReportError(new InputFieldsAndArgumentsOfCorrectLengthError(context, node, null, (int?)min, (int?)max));
            }
            else if (node.Value is StringValue strLiteral)
            {
                CheckStringLength(strLiteral.Value);
            }
            else if (node.Value is VariableReference vRef && context.Inputs != null)
            {
                if (context.Inputs.TryGetValue(vRef.Name, out var value))
                {
                    if (value is string strVariable)
                        CheckStringLength(strVariable);
                    else if (value is null && min != null)
                        context.ReportError(new InputFieldsAndArgumentsOfCorrectLengthError(context, node, null, (int?)min, (int?)max));
                }
            }

            void CheckStringLength(string str)
            {
                if (min != null && str.Length < (int)min || max != null && str.Length > (int)max)
                    context.ReportError(new InputFieldsAndArgumentsOfCorrectLengthError(context, node, str.Length, (int?)min, (int?)max));
            }
        }
    }
}
