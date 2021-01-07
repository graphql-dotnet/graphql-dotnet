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
        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            var knownArgs = new Dictionary<string, Argument>();

            return new EnterLeaveListener(_ =>
            {
                _.Match<Field>(__ => knownArgs = new Dictionary<string, Argument>());
                _.Match<Directive>(__ => knownArgs = new Dictionary<string, Argument>());

                _.Match<Argument>(argument =>
                {
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
        }
    }
}
