using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation.Errors;

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
        public static readonly VariablesAreInputTypes Instance = new VariablesAreInputTypes();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<VariableDefinition>(varDef =>
                {
                    var type = varDef.Type.GraphTypeFromType(context.Schema);

                    if (!type.IsInputType())
                    {
                        context.ReportError(new VariablesAreInputTypesError(context, varDef, type));
                    }
                });
            }).ToTask();
        }
    }
}
