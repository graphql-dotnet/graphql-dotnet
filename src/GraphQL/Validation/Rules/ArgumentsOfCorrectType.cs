using System.Collections.Generic;
using System.Linq;
using GraphQL.Language;
using GraphQL.Types;

namespace GraphQL.Validation.Rules
{
    public class ArgumentsOfCorrectType : IValidationRule
    {
        public INodeVisitor Validate(ValidationContext context)
        {
            return new NodeVisitorMatchFuncListener<Argument>(
                n => n is Argument,
                argAst =>
                {
                    var argDef = context.TypeInfo.GetArgument();

                    if (argDef != null)
                    {
                        var type = context.Schema.FindType(argDef.Type);
                        var errors = type.IsValidLiteralValue(argAst.Value, context.Schema).ToList();
                        if (errors.Any())
                        {
                            var error = new ValidationError(
                                "5.3.3.1",
                                BadValueMessage(argAst.Name, type, context.Print(argAst.Value), errors));
                            error.AddLocation(argAst.SourceLocation.Line, argAst.SourceLocation.Column);
                            context.ReportError(error);
                        }
                    }
                });
        }

        public string BadValueMessage(
            string argName,
            GraphType type,
            string value,
            IEnumerable<string> verboseErrors)
        {
            var message = verboseErrors != null ? $"\n{string.Join("\n", verboseErrors)}" : "";

            return $"Argument \"{argName}\" has invalid value {value}.{message}";
        }
    }
}
