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

        private static readonly Task<INodeVisitor> _task = new EnterLeaveListener(_ =>
            {
                _.Match<Field>((__, context) => context.Set<UniqueArgumentNames>(new Dictionary<string, Argument>()));
                _.Match<Directive>((__, context) => context.Set<UniqueArgumentNames>(new Dictionary<string, Argument>()));

                _.Match<Argument>((argument, context) =>
                {
                    var knownArgs = context.Get<UniqueArgumentNames, Dictionary<string, Argument>>();
                    var argName = argument.Name;
                    if (knownArgs.ContainsKey(argName))
                    {
                        context.ReportError(new UniqueArgumentNamesError(context, knownArgs[argName], argument));
                    }
                    else
                    {
                        knownArgs[argName] = argument;
                    }
                });
            }).ToTask();

        /// <inheritdoc/>
        /// <exception cref="UniqueArgumentNamesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;
    }
}
