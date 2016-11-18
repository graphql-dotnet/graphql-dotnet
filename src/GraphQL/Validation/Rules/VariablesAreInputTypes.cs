using System;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Variables are input types
    ///
    /// A GraphQL operation is only valid if all the variables it defines are of
    /// input types (scalar, enum, or input object).
    /// </summary>
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

                    if (!type.IsInputType())
                    {
                        context.ReportError(new ValidationError(context.OriginalQuery, "5.7.3", UndefinedVarMessage(varDef.Name, type != null ? context.Print(type) : varDef.Type.Name()), varDef));
                    }
                });
            });
        }
    }
}
