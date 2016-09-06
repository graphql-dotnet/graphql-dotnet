using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique fragment names
    /// 
    /// A GraphQL document is only valid if all defined fragments have unique names.
    /// </summary>
    public class UniqueFragmentNames : IValidationRule
  {
    public string DuplicateFragmentNameMessage(string fragName)
    {
      return $"There can only be one fragment named \"{fragName}\"";
    }

    public INodeVisitor Validate(ValidationContext context)
    {
      var knownFragments = new Dictionary<string, FragmentDefinition>();

      return new EnterLeaveListener(_ =>
      {
        _.Match<FragmentDefinition>(fragmentDefinition =>
        {
          var fragmentName = fragmentDefinition.Name;
          if (knownFragments.ContainsKey(fragmentName))
          {
              var error = new ValidationError(
                  context.OriginalQuery,
                  "5.4.1.1",
                  DuplicateFragmentNameMessage(fragmentName),
                  knownFragments[fragmentName],
                  fragmentDefinition);
            context.ReportError(error);
          }
          else
          {
            knownFragments[fragmentName] = fragmentDefinition;
          }
        });
      });
    }
  }
}
