using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    public class UniqueFragmentNamesError : ValidationError
    {
        internal const string NUMBER = "5.5.1.1";

        public UniqueFragmentNamesError(ValidationContext context, FragmentDefinition node, FragmentDefinition altNode)
            : base(context.OriginalQuery, NUMBER, DuplicateFragmentNameMessage(node.Name), node, altNode)
        {
        }

        internal static string DuplicateFragmentNameMessage(string fragName)
            => $"There can only be one fragment named \"{fragName}\"";
    }
}
