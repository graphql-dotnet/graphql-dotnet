using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Variable default values of correct type
    ///
    /// A GraphQL document is only valid if all variable default values are of the
    /// type expected by their definition.
    /// </summary>
    public class DefaultValuesOfCorrectType : IValidationRule
    {
        public readonly Func<string, string, string, string> BadValueForNonNullArgMessage =
            (varName, type, guessType) => $"Variable \"{varName}\" of type \"{type}\" is required and" +
                                          " will not use default value. " +
                                          $"Perhaps you mean to use type \"{guessType}\"?";

        public readonly Func<string, string, string, IEnumerable<string>, string> BadValueForDefaultArgMessage =
            (varName, type, value, verboseErrors) =>
            {
                var message = verboseErrors != null ? "\n" + string.Join("\n", verboseErrors) : "";
                return $"Variable \"{varName}\" of type \"{type}\" has invalid default value {value}.{message}";
            };

        public static readonly DefaultValuesOfCorrectType Instance = new DefaultValuesOfCorrectType();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<VariableDefinition>(varDefAst =>
                {
                    var name = varDefAst.Name;
                    var defaultValue = varDefAst.DefaultValue;
                    var inputType = context.TypeInfo.GetInputType();

                    if (inputType is NonNullGraphType nonNullType && defaultValue != null)
                    {
                        context.ReportError(new ValidationError(
                            context.OriginalQuery,
                            "5.7.2",
                            BadValueForNonNullArgMessage(
                                name,
                                context.Print(nonNullType),
                                context.Print(nonNullType.ResolvedType)),
                            defaultValue));
                    }

                    if (inputType != null && defaultValue != null)
                    {
                        var errors = inputType.IsValidLiteralValue(defaultValue, context.Schema).ToList();
                        if (errors.Count > 0)
                        {
                            context.ReportError(new ValidationError(
                                context.OriginalQuery,
                                "5.7.2",
                                BadValueForDefaultArgMessage(
                                    name,
                                    context.Print(inputType),
                                    context.Print(defaultValue),
                                    errors),
                                defaultValue));
                        }
                    }
                });
            }).ToTask();
        }
    }
}
