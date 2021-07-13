#nullable enable

using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Variable default values of correct type:
    ///
    /// A GraphQL document is only valid if all variable default values are of the
    /// type expected by their definition.
    /// </summary>
    public class DefaultValuesOfCorrectType : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly DefaultValuesOfCorrectType Instance = new DefaultValuesOfCorrectType();

        /// <inheritdoc/>
        /// <exception cref="DefaultValuesOfCorrectTypeError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _nodeVisitor;

        private static readonly Task<INodeVisitor> _nodeVisitor = new MatchingNodeVisitor<VariableDefinition>((varDefAst, context) =>
        {
            var defaultValue = varDefAst.DefaultValue;
            var inputType = context.TypeInfo.GetInputType();

            if (inputType != null && defaultValue != null)
            {
                var errors = context.IsValidLiteralValue(inputType, defaultValue);
                if (errors != null)
                {
                    context.ReportError(new DefaultValuesOfCorrectTypeError(context, varDefAst, inputType, errors));
                }
            }
        }).ToTask();
    }
}
