using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Argument values of correct type
    ///
    /// A GraphQL document is only valid if all field argument literal values are
    /// of the type expected by their position.
    /// </summary>
    public class ArgumentsOfCorrectType : IValidationRule
    {
        public static readonly ArgumentsOfCorrectType Instance = new ArgumentsOfCorrectType();

        private static readonly Task<INodeVisitor> _task = new MatchingNodeVisitor<Argument>((argAst, context) =>
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
                }).ToTask();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;
    }
}
