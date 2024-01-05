using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    internal class NoConnectionOver1000ValidationRule : IValidationRule, IVariableVisitorProvider, INodeVisitor
    {
        // do not run any visitors on the initial validation
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => default;

        // only run this rule when there are argument values specify
        ValueTask<INodeVisitor?> IVariableVisitorProvider.ValidateArgumentsAsync(ValidationContext context)
            => context.ArgumentValues != null ? new(this) : default;

        // not using IVariableVisitor
        IVariableVisitor? IVariableVisitorProvider.GetVisitor(ValidationContext context) => null;

        // look for connection arguments and validate the value
        ValueTask INodeVisitor.EnterAsync(ASTNode node, ValidationContext context)
        {
            // look for field nodes and find the field definition
            if (node is not GraphQLField fieldNode)
                return default;
            var fieldDef = context.TypeInfo.GetFieldDef();
            if (fieldDef == null)
                return default;
            // look for connection types
            if (fieldDef.ResolvedType?.GetNamedType() is not IObjectGraphType connectionType || !connectionType.Name.EndsWith("Connection"))
                return default;
            // retrieve the arguments
            if (!(context.ArgumentValues?.TryGetValue(fieldNode, out var args) ?? false))
                return default;
            // look for first and last arguments
            ArgumentValue lastArg = default;
            if (!args.TryGetValue("first", out var firstArg) && !args.TryGetValue("last", out lastArg))
                return default;
            var rows = (int?)firstArg.Value ?? (int?)lastArg.Value ?? 0;
            if (rows > 1000)
                context.ReportError(new ValidationError("Cannot return more than 1000 rows"));
            return default;
        }

        ValueTask INodeVisitor.LeaveAsync(ASTNode node, ValidationContext context) => default;
    }
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

            public override ValueTask VisitFieldAsync(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, IInputObjectGraphType type, FieldType field, object? variableValue, object? parsedValue)
            {
                var lengthDirective = field.FindAppliedDirective("length");
                if (lengthDirective == null)
                    return default;

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

                return default;
            }
        }

        /// <inheritdoc/>
        public IVariableVisitor GetVisitor(ValidationContext _) => FieldVisitor.Instance;

        /// <inheritdoc/>
        public ValueTask<INodeVisitor?> ValidateArgumentsAsync(ValidationContext _) => default;

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
                if (context.TypeInfo.GetInputType(1)?.GetNamedType() is IInputObjectGraphType input)
                    CheckLength(field, field.Value, input.GetField(field.Name), context);
            })
        );

        private static void CheckLength(ASTNode node, GraphQLValue value, IMetadataReader? provider, ValidationContext context)
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
