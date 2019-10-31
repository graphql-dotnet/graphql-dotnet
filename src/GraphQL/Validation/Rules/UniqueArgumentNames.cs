using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique argument names
    ///
    /// A GraphQL field or directive is only valid if all supplied arguments at a given field
    /// are uniquely named.
    /// </summary>
    public class UniqueArgumentNames : IValidationRule
    {
        public string DuplicateArgMessage(string argName)
        {
            return $"There can be only one argument named \"{argName}\".";
        }

        public static readonly UniqueArgumentNames Instance = new UniqueArgumentNames();

        public INodeVisitor Validate(ValidationContext context)
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
                        var error = new ValidationError(context.OriginalQuery,
                            "5.3.2",
                            DuplicateArgMessage(argName),
                            knownArgs[argName],
                            argument);
                        context.ReportError(error);
                    }
                    else
                    {
                        knownArgs[argName] = argument;
                    }
                });
            });
        }
    }
}
