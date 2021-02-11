using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks minimum and maximum length of provided values for input fields and
    /// arguments that marked with <see cref="LengthDirective"/> directive. Doesn't check default values.
    ///
    /// <br/><br/>
    /// This is not a standard validation rule that is not in the specification.
    /// </summary>
    public class InputFieldsAndArgumentsOfCorrectLength : IValidationRule
    {
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

        private static void CheckLength(IHaveValue node, IProvideMetadata provider, ValidationContext context)
        {
            if (provider == null || !provider.HasAppliedDirectives())
                return;

            var lengthDirective = provider.GetAppliedDirectives().Find("length");
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
                var operation = string.IsNullOrWhiteSpace(context.OperationName)
                    ? context.Document.Operations.FirstOrDefault()
                    : context.Document.Operations.WithName(context.OperationName);

                //TODO: change to get single variable
                var values = ExecutionHelper.GetVariableValues(context.Document, context.Schema, operation?.Variables, context.Inputs);
                if (values.ValueFor(vRef.Name, out var argValue))
                {
                    if (argValue.Value is string strVariable)
                        CheckStringLength(strVariable);
                    else if (argValue.Value is null && min != null)
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
