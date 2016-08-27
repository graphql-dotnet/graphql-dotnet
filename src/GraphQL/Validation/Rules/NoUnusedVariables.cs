using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// No unused variables
    /// 
    /// A GraphQL operation is only valid if all variables defined by that operation
    /// are used in that operation or a fragment transitively included by that
    /// operation. 
    /// </summary>
    public class NoUnusedVariables : IValidationRule
  {
    public string UnusedVariableMessage(string varName, string opName)
    {
      return !string.IsNullOrWhiteSpace(opName)
        ? $"Variable \"${varName}\" is never used in operation \"${opName}\"."
        : $"Variable \"${varName}\" is never used.";
    }

    public INodeVisitor Validate(ValidationContext context)
    {
      var variableDefs = new List<VariableDefinition>();
      
      return new EnterLeaveListener(_ =>
      {
        _.Match<VariableDefinition>(def => variableDefs.Add(def));

        _.Match<Operation>(
          enter: op => variableDefs = new List<VariableDefinition>(),
          leave: op =>
          {
            var usages = context.GetRecursiveVariables(op).Select(usage => usage.Node.Name);
            variableDefs.Apply(variableDef =>
            {
              var variableName = variableDef.Name;
              if (!usages.Contains(variableName))
              {
                var error = new ValidationError(context.OriginalQuery, "5.7.5", UnusedVariableMessage(variableName, op.Name), variableDef);
                context.ReportError(error);
              }
            });
          });
      });
    }
  }
}
