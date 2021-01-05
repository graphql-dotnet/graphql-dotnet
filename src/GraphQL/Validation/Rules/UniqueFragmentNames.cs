using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique fragment names:
    ///
    /// A GraphQL document is only valid if all defined fragments have unique names.
    /// </summary>
    public class UniqueFragmentNames : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly UniqueFragmentNames Instance = new UniqueFragmentNames();

        private static readonly Task<INodeVisitor> _task = new EnterLeaveListener(_ =>
            {
                _.Match<Document>((_, context) => context.Set<UniqueFragmentNames>(new Dictionary<string, FragmentDefinition>()));
                _.Match<FragmentDefinition>((fragmentDefinition, context) =>
                {
                    var knownFragments = context.Get<UniqueFragmentNames, Dictionary<string, FragmentDefinition>>();
                    var fragmentName = fragmentDefinition.Name;
                    if (knownFragments.ContainsKey(fragmentName))
                    {
                        context.ReportError(new UniqueFragmentNamesError(context, knownFragments[fragmentName], fragmentDefinition));
                    }
                    else
                    {
                        knownFragments[fragmentName] = fragmentDefinition;
                    }
                });
            },
            shouldRun: context => context.Document.Fragments.Count >= 2).ToTask();

        /// <inheritdoc/>
        /// <exception cref="UniqueFragmentNamesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;
    }
}
