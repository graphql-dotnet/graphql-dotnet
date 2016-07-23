using System;
using GraphQL.Language;
using GraphQL.Types;

namespace GraphQL.Validation.Rules
{
    public class VariablesAreInputTypes : IValidationRule
    {
        public Func<string, string, string> UndefinedVarMessage = (variableName, typeName) =>
            $"Variable \"{variableName}\" cannot be non-input type \"{typeName}\".";

        public INodeVisitor Validate(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<VariableDefinition>(varDef =>
                {
                    var type = varDef.Type.GraphTypeFromType(context.Schema);

                    if (!type.IsInputType(context.Schema))
                    {
                        context.ReportError(new ValidationError("", UndefinedVarMessage(varDef.Name, context.Print(type)), varDef));
                    }
                });
            });
        }
    }
}
