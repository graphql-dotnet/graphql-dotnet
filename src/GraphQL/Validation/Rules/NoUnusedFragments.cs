using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// No unused fragments
    /// 
    /// A GraphQL document is only valid if all fragment definitions are spread
    /// within operations, or spread within other fragments spread within operations.
    /// </summary>
    public class NoUnusedFragments : IValidationRule
  {
    public string UnusedFragMessage(string fragName)
    {
      return $"Fragment \"{fragName}\" is never used.";
    }

    public INodeVisitor Validate(ValidationContext context)
    {
      var operationDefs = new List<Operation>();
      var fragmentDefs = new List<FragmentDefinition>();

      return new EnterLeaveListener(_ =>
      {
        _.Match<Operation>(node => operationDefs.Add(node));
        _.Match<FragmentDefinition>(node => fragmentDefs.Add(node));
        _.Match<Document>(
          leave: document =>
          {
            var fragmentNameUsed = new List<string>();
            operationDefs.Apply(operation =>
            {
              context.GetRecursivelyReferencedFragments(operation).Apply(fragment =>
              {
                fragmentNameUsed.Add(fragment.Name);
              });
            });

            fragmentDefs.Apply(fragmentDef =>
            {
              var fragName = fragmentDef.Name;
              if (!fragmentNameUsed.Contains(fragName))
              {
                var error = new ValidationError(context.OriginalQuery, "5.4.1.4", UnusedFragMessage(fragName), fragmentDef);
                context.ReportError(error);
              }
            });
          });
      });
    }
  }
}
