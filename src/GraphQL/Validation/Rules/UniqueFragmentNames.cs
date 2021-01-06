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

        /// <inheritdoc/>
        /// <exception cref="UniqueFragmentNamesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => context.Document.Fragments.Count > 1 ? _nodeVisitor : null;

        private static readonly Task<INodeVisitor> _nodeVisitor = new MatchingNodeVisitor<FragmentDefinition>((fragmentDefinition, context) =>
            {
                var knownFragments = context.TypeInfo.UniqueFragmentNames_KnownFragments ??= new Dictionary<string, FragmentDefinition>();

                var fragmentName = fragmentDefinition.Name;
                if (knownFragments.ContainsKey(fragmentName)) // .NET 2.2+ has TryAdd
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
