using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public static readonly NoUnusedFragments Instance = new NoUnusedFragments();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            var operationDefs = new List<Operation>();
            var fragmentDefs = new List<FragmentDefinition>();

            return new EnterLeaveListener(_ =>
            {
                _.Match<Operation>(node => operationDefs.Add(node));
                _.Match<FragmentDefinition>(node => fragmentDefs.Add(node));
                _.Match<Document>(leave: document =>
                {
                    var fragmentNamesUsed = operationDefs
                        .SelectMany(context.GetRecursivelyReferencedFragments)
                        .Select(fragment => fragment.Name)
                        .ToList();

                    foreach (var fragmentDef in fragmentDefs)
                    {
                        var fragName = fragmentDef.Name;

                        if (!fragmentNamesUsed.Contains(fragName))
                        {
                            var error = new ValidationError(context.OriginalQuery, "5.4.1.4", UnusedFragMessage(fragName), fragmentDef);
                            context.ReportError(error);
                        }
                    }
                });
            }).ToTask();
        }
    }
}
