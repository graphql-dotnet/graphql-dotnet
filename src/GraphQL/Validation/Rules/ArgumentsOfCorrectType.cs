using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Argument values of correct type:
    ///
    /// A GraphQL document is only valid if all field argument literal values are
    /// of the type expected by their position.
    /// </summary>
    public class ArgumentsOfCorrectType : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly ArgumentsOfCorrectType Instance = new ArgumentsOfCorrectType();

        /// <inheritdoc/>
        /// <exception cref="ArgumentsOfCorrectTypeError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<Argument>(argAst =>
                {
                    var argDef = context.TypeInfo.GetArgument();
                    if (argDef == null)
                        return;

                    var type = argDef.ResolvedType;
                    var errors = type.IsValidLiteralValue(argAst.Value ?? argDef.GetDefaultValueAST(context.Schema), context.Schema).ToList();
                    if (errors.Count > 0)
                    {
                        context.ReportError(new ArgumentsOfCorrectTypeError(context, argAst, errors));
                    }
                });
            }).ToTask();
        }
    }
}
