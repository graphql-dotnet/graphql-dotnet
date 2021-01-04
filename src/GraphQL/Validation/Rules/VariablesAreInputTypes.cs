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
                    var type = GetNamedGraphTypeFromType(varDef.Type, context.Schema);

                    if (!type.IsInputType())
                    {
                        context.ReportError(new VariablesAreInputTypesError(context, varDef, varDef.Type.GraphTypeFromType(context.Schema)));
                    }
                });
            }).ToTask();
        }

        private static IGraphType GetNamedGraphTypeFromType(IType type, ISchema schema) => type switch
        {
            NonNullType nonnull => GetNamedGraphTypeFromType(nonnull.Type, schema),
            ListType list => GetNamedGraphTypeFromType(list.Type, schema),
            NamedType named => schema.FindType(named.Name),
            _ => null
        };
    }
}
