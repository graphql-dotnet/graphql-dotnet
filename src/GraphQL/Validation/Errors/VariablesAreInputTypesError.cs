using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Errors
{
    public class VariablesAreInputTypesError : ValidationError
    {
        public const string PARAGRAPH = "5.8.2";

        public VariablesAreInputTypesError(ValidationContext context, VariableDefinition node, IGraphType type)
            : base(context.OriginalQuery, PARAGRAPH, UndefinedVarMessage(node.Name, type != null ? context.Print(type) : node.Type.Name()), node)
        {
        }

        internal static string UndefinedVarMessage(string variableName, string typeName)
            => $"Variable \"{variableName}\" cannot be non-input type \"{typeName}\".";
    }
}
