using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Variables are input types:
    ///
    /// A GraphQL operation is only valid if all the variables it defines are of
    /// input types (scalar, enum, or input object).
    /// </summary>
    public class VariablesAreInputTypes : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly VariablesAreInputTypes Instance = new VariablesAreInputTypes();

        private static readonly Task<INodeVisitor> _task = new MatchingNodeVisitor<VariableDefinition>((varDef, context) =>
                {
                    var type = GetNamedGraphTypeFromType(varDef.Type, context.Schema);

                    if (!type.IsInputType())
                    {
                        context.ReportError(new VariablesAreInputTypesError(context, varDef, varDef.Type.GraphTypeFromType(context.Schema)));
                    }
                }).ToTask();

        /// <inheritdoc/>
        /// <exception cref="VariablesAreInputTypesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;

        private static IGraphType GetNamedGraphTypeFromType(IType type, ISchema schema) => type switch
        {
            NonNullType nonnull => GetNamedGraphTypeFromType(nonnull.Type, schema),
            ListType list => GetNamedGraphTypeFromType(list.Type, schema),
            NamedType named => schema.FindType(named.Name),
            _ => null
        };
    }
}
