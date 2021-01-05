using System.Linq;
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
        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<VariableDefinition>(varDefAst =>
                {
                    var defaultValue = varDefAst.DefaultValue;
                    var inputType = context.TypeInfo.GetInputType();

                    if (inputType != null && defaultValue != null)
                    {
                        var errors = inputType.IsValidLiteralValue(defaultValue, context.Schema).ToList();
                        if (errors.Count > 0)
                        {
                            context.ReportError(new DefaultValuesOfCorrectTypeError(context, varDefAst, inputType, errors));
                        }
                    }
                });
            }).ToTask();
        }
    }
}
