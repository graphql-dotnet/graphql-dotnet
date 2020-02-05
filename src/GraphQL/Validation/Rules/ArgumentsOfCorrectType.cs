using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;

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

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<Argument>(argAst =>
                {
                    var argDef = context.TypeInfo.GetArgument();
                    if (argDef == null) return;

                    var type = argDef.ResolvedType;
                    var errors = type.IsValidLiteralValue(argAst.Value, context.Schema).ToList();
                    if (errors.Count > 0)
                    {
                        var error = new ValidationError(
                            context.OriginalQuery,
                            "5.3.3.1",
                            BadValueMessage(argAst.Name, type, context.Print(argAst.Value), errors),
                            argAst);
                        context.ReportError(error);
                    }
                });
            }).ToTask();
        }

        public string BadValueMessage(
            string argName,
            IGraphType type,
            string value,
            IEnumerable<string> verboseErrors)
        {
            var message = verboseErrors != null ? $"\n{string.Join("\n", verboseErrors)}" : "";

            return $"Argument \"{argName}\" has invalid value {value}.{message}";
        }
    }
}
