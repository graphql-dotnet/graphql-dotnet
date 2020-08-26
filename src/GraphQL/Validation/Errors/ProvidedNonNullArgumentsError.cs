using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Errors
{
    public class ProvidedNonNullArgumentsError : ValidationError
    {
        public const string PARAGRAPH = "5.4.2.1";

        public ProvidedNonNullArgumentsError(ValidationContext context, Field node, QueryArgument arg)
            : base(context.OriginalQuery, PARAGRAPH, MissingFieldArgMessage(node.Name, arg.Name, context.Print(arg.ResolvedType)), node)
        {
        }

        public ProvidedNonNullArgumentsError(ValidationContext context, Directive node, QueryArgument arg)
            : base(context.OriginalQuery, PARAGRAPH, MissingDirectiveArgMessage(node.Name, arg.Name, context.Print(arg.ResolvedType)), node)
        {
        }

        internal static string MissingFieldArgMessage(string fieldName, string argName, string type)
            => $"Argument \"{argName}\" of type \"{type}\" is required for field \"{fieldName}\" but not provided.";

        internal static string MissingDirectiveArgMessage(string directiveName, string argName, string type)
            => $"Argument \"{argName}\" of type \"{type}\" is required for directive \"{directiveName}\" but not provided.";
    }
}
