using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// No unused fragments:
    ///
    /// A GraphQL document is only valid if all fragment definitions are spread
    /// within operations, or spread within other fragments spread within operations.
    /// </summary>
    public class NoUnusedFragments : IValidationRule
    {
        private sealed class NoUnusedFragmentsData
        {
            public List<Operation> OperationDefs = new List<Operation>();

            public List<FragmentDefinition> FragmentDefs = new List<FragmentDefinition>();
        }

        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly NoUnusedFragments Instance = new NoUnusedFragments();

        private static readonly Task<INodeVisitor> _task = new EnterLeaveListener(_ =>
            {
                _.Match<Operation>((node, context) => context.Get<NoUnusedFragments, NoUnusedFragmentsData>().OperationDefs.Add(node));
                _.Match<FragmentDefinition>((node, context) => context.Get<NoUnusedFragments, NoUnusedFragmentsData>().FragmentDefs.Add(node));
                _.Match<Document>(
                    enter: (_, context) => context.Set<NoUnusedFragments>(new NoUnusedFragmentsData()),
                    leave: (document, context) =>
                    {
                        var data = context.Get<NoUnusedFragments, NoUnusedFragmentsData>();
                        var fragmentNamesUsed = data.OperationDefs
                                .SelectMany(context.GetRecursivelyReferencedFragments)
                                .Select(fragment => fragment.Name)
                                .ToList();

                        foreach (var fragmentDef in data.FragmentDefs)
                        {
                            var fragName = fragmentDef.Name;

                            if (!fragmentNamesUsed.Contains(fragName))
                            {
                                context.ReportError(new NoUnusedFragmentsError(context, fragmentDef));
                            }
                        }
                    });
            }).ToTask();

        /// <inheritdoc/>
        /// <exception cref="NoUnusedFragmentsError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;
    }
}
