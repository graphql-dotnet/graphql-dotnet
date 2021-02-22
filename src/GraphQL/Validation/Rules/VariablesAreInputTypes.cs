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

        /// <inheritdoc/>
        /// <exception cref="VariablesAreInputTypesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _nodeVisitor;

        private static readonly Task<INodeVisitor> _nodeVisitor = new MatchingNodeVisitor<VariableDefinition>((varDef, context) =>
        {
            var type = varDef.Type.NamedGraphTypeFromType(context.Schema);

            if (!type.IsInputType())
            {
                context.ReportError(new VariablesAreInputTypesError(context, varDef, varDef.Type.GraphTypeFromType(context.Schema)));
            }
        }).ToTask();
    }
}
