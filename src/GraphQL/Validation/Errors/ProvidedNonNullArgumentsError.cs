using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Errors
{
    public class ProvidedNonNullArgumentsError : ValidationError
    {
        internal const string NUMBER = "5.4.2.1";

        public ProvidedNonNullArgumentsError(ValidationContext context, Field node, QueryArgument arg)
            : base(context.OriginalQuery, NUMBER, MissingFieldArgMessage(node.Name, arg.Name, context.Print(arg.ResolvedType)), node)
        {
        }

        public ProvidedNonNullArgumentsError(ValidationContext context, Directive node, QueryArgument arg)
            : base(context.OriginalQuery, NUMBER, MissingDirectiveArgMessage(node.Name, arg.Name, context.Print(arg.ResolvedType)), node)
        {
        }

        internal static string MissingFieldArgMessage(string fieldName, string argName, string type)
            => $"Argument \"{argName}\" of type \"{type}\" is required for field \"{fieldName}\" but not provided.";

        internal static string MissingDirectiveArgMessage(string directiveName, string argName, string type)
            => $"Argument \"{argName}\" of type \"{type}\" is required for directive \"{directiveName}\" but not provided.";
    }
}
