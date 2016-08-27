using GraphQL.Language.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Known fragment names
    /// 
    /// A GraphQL document is only valid if all <c>...Fragment</c> fragment spreads refer
    /// to fragments defined in the same document.
    /// </summary>
    public class KnownFragmentNames : IValidationRule
  {
    public string UnknownFragmentMessage(string fragName)
    {
      return $"Unknown fragment \"{fragName}\".";
    }

    public INodeVisitor Validate(ValidationContext context)
    {
      return new EnterLeaveListener(_ =>
      {
        _.Match<FragmentSpread>(node =>
        {
          var fragmentName = node.Name;
          var fragment = context.GetFragment(fragmentName);
          if (fragment == null)
          {
            var error = new ValidationError(context.OriginalQuery, "5.4.2.1", UnknownFragmentMessage(fragmentName), node);
            context.ReportError(error);
          }
        });
      });
    }
  }
}
