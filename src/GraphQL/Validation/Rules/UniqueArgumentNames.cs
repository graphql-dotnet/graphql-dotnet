using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique argument names:
    ///
    /// A GraphQL field or directive is only valid if all supplied arguments at a given field
    /// are uniquely named.
    /// </summary>
    public class UniqueArgumentNames : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly UniqueArgumentNames Instance = new UniqueArgumentNames();

        /// <inheritdoc/>
        /// <exception cref="UniqueArgumentNamesError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new ValueTask<INodeVisitor?>(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<Field>((__, context) => context.TypeInfo.UniqueArgumentNames_KnownArgs?.Clear()),
            new MatchingNodeVisitor<Directive>((__, context) => context.TypeInfo.UniqueArgumentNames_KnownArgs?.Clear()),
            new MatchingNodeVisitor<Argument>((argument, context) =>
            {
                var knownArgs = context.TypeInfo.UniqueArgumentNames_KnownArgs ??= new Dictionary<string, Argument>();
                string argName = argument.Name;
                if (knownArgs.ContainsKey(argName))
                {
                    context.ReportError(new UniqueArgumentNamesError(context, knownArgs[argName], argument));
                }
                else
                {
                    knownArgs[argName] = argument;
                }
            })
        );
    }
}
