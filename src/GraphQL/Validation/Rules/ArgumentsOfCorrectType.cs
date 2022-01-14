using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Argument values of correct type:
    ///
    /// A GraphQL document is only valid if all field argument literal values are
    /// of the type expected by their position.
    /// </summary>
    public class ArgumentsOfCorrectType : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly ArgumentsOfCorrectType Instance = new ArgumentsOfCorrectType();

        /// <inheritdoc/>
        /// <exception cref="ArgumentsOfCorrectTypeError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new ValueTask<INodeVisitor?>(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLArgument>((argAst, context) =>
        {
            var argDef = context.TypeInfo.GetArgument();
            if (argDef == null)
                return;

            var type = argDef.ResolvedType!;
            var errors = context.IsValidLiteralValue(type, (IValue)argAst.Value);
            if (errors != null)
            {
                context.ReportError(new ArgumentsOfCorrectTypeError(context, argAst, errors));
            }
        });
    }
}
