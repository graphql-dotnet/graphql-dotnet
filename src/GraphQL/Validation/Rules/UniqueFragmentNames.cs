using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique fragment names
    ///
    /// A GraphQL document is only valid if all defined fragments have unique names.
    /// </summary>
    public class UniqueFragmentNames : IValidationRule
    {
        public static readonly UniqueFragmentNames Instance = new UniqueFragmentNames();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            var knownFragments = new Dictionary<string, FragmentDefinition>();

            return new MatchingNodeVisitor<FragmentDefinition>(fragmentDefinition =>
                {
                    var fragmentName = fragmentDefinition.Name;
                    if (knownFragments.ContainsKey(fragmentName))
                    {
                        context.ReportError(new UniqueFragmentNamesError(context, knownFragments[fragmentName], fragmentDefinition));
                    }
                    else
                    {
                        knownFragments[fragmentName] = fragmentDefinition;
                    }
                }).ToTask();
        }
    }
}
