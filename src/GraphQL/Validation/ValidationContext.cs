using System.Collections.Generic;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Validation
{
    public class ValidationContext
    {
        private readonly List<ValidationError> _errors = new List<ValidationError>();

        private readonly Dictionary<Operation, IEnumerable<FragmentDefinition>> _fragments
            = new Dictionary<Operation, IEnumerable<FragmentDefinition>>();

        private readonly Dictionary<Operation, IEnumerable<VariableUsage>> _variables =
            new Dictionary<Operation, IEnumerable<VariableUsage>>();

        public string OriginalQuery { get; set; }

        public string OperationName { get; set; }

        public ISchema Schema { get; set; }

        public Document Document { get; set; }

        public TypeInfo TypeInfo { get; set; }

        public IDictionary<string, object> UserContext { get; set; }

        public IEnumerable<ValidationError> Errors => _errors;

        public Inputs Inputs { get; set; }

        public void ReportError(ValidationError error)
        {
            Invariant.Check(error != null, "Must provide a validation error.");
            _errors.Add(error);
        }

        public List<VariableUsage> GetVariables(IHaveSelectionSet node)
        {
            var usages = new List<VariableUsage>();
            var info = new TypeInfo(Schema);

            var listener = new EnterLeaveListener(_ =>
            {
                _.Match<VariableReference>(
                    varRef => usages.Add(new VariableUsage(varRef, info.GetInputType()))
                );
            });

            var visitor = new BasicVisitor(info, listener);
            visitor.Visit(node);

            return usages;
        }

        public IEnumerable<VariableUsage> GetRecursiveVariables(Operation operation)
        {
            if (_variables.TryGetValue(operation, out var results))
            {
                return results;
            }

            var usages = GetVariables(operation);

            foreach (var fragment in GetRecursivelyReferencedFragments(operation))
            {
                usages.AddRange(GetVariables(fragment));
            }

            _variables[operation] = usages;

            return usages;
        }

        public FragmentDefinition GetFragment(string name)
        {
            return Document.Fragments.FindDefinition(name);
        }

        public List<FragmentSpread> GetFragmentSpreads(SelectionSet node)
        {
            var spreads = new List<FragmentSpread>();

            var setsToVisit = new Stack<SelectionSet>(new[] { node });

            while (setsToVisit.Count > 0)
            {
                var set = setsToVisit.Pop();

                foreach (var selection in set.Selections)
                {
                    if (selection is FragmentSpread spread)
                    {
                        spreads.Add(spread);
                    }
                    else if (selection is IHaveSelectionSet hasSet)
                    {
                        if (hasSet.SelectionSet != null)
                        {
                            setsToVisit.Push(hasSet.SelectionSet);
                        }
                    }
                }
            }

            return spreads;
        }

        public IEnumerable<FragmentDefinition> GetRecursivelyReferencedFragments(Operation operation)
        {
            if (_fragments.TryGetValue(operation, out var results))
            {
                return results;
            }

            var fragments = new List<FragmentDefinition>();
            var nodesToVisit = new Stack<SelectionSet>(new[] {operation.SelectionSet});
            var collectedNames = new Dictionary<string, bool>();

            while (nodesToVisit.Count > 0)
            {
                var node = nodesToVisit.Pop();

                foreach (var spread in GetFragmentSpreads(node))
                {
                    var fragName = spread.Name;
                    if (!collectedNames.ContainsKey(fragName))
                    {
                        collectedNames[fragName] = true;

                        var fragment = GetFragment(fragName);
                        if (fragment != null)
                        {
                            fragments.Add(fragment);
                            nodesToVisit.Push(fragment.SelectionSet);
                        }
                    }
                }
            }

            _fragments[operation] = fragments;

            return fragments;
        }

        public string Print(INode node)
        {
            return AstPrinter.Print(node);
        }

        public string Print(IGraphType type)
        {
            return SchemaPrinter.ResolveName(type);
        }
    }

    public class VariableUsage
    {
        public VariableReference Node { get; }
        public IGraphType Type { get; }

        public VariableUsage(VariableReference node, IGraphType type)
        {
            Node = node;
            Type = type;
        }
    }
}
