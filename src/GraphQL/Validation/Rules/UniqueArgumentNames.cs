using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Rules
{
    public class UniqueArgumentNames : IValidationRule
  {
    public string DuplicateArgMessage(string argName)
    {
      return $"There can be only one argument named \"{argName}\".";
    }

    public INodeVisitor Validate(ValidationContext context)
    {
      var knownArgs = new Dictionary<string, Argument>();

      return new EnterLeaveListener(_ =>
      {
        _.Match<Field>(field => knownArgs = new Dictionary<string, Argument>());
        _.Match<Directive>(field => knownArgs = new Dictionary<string, Argument>());

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
