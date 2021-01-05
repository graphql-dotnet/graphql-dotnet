using System;
using System.Collections.Generic;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Validation
{
    /// <summary>
    /// Provides contextual information about the validation of the document.
    /// </summary>
    public class ValidationContext : IProvideUserContext
    {
        private readonly List<ValidationError> _errors = new List<ValidationError>();

        private readonly Dictionary<Operation, IEnumerable<FragmentDefinition>> _fragments
            = new Dictionary<Operation, IEnumerable<FragmentDefinition>>();

        private readonly Dictionary<Operation, IEnumerable<VariableUsage>> _variables =
            new Dictionary<Operation, IEnumerable<VariableUsage>>();

        /// <summary>
        /// Returns the original GraphQL query string.
        /// </summary>
        public string OriginalQuery { get; set; }

        /// <summary>
        /// Returns the operation name requested to be executed.
        /// </summary>
        public string OperationName { get; set; }

        /// <inheritdoc cref="ExecutionContext.Schema"/>
        public ISchema Schema { get; set; }

        /// <inheritdoc cref="ExecutionContext.Document"/>
        public Document Document { get; set; }

        public TypeInfo TypeInfo { get; set; }

        /// <inheritdoc/>
        public IDictionary<string, object> UserContext { get; set; }

        /// <summary>
        /// Returns a list of validation errors for this document.
        /// </summary>
        public IEnumerable<ValidationError> Errors => _errors;

        /// <summary>
        /// Returns <see langword="true"/> if there are any validation errors for this document.
        /// </summary>
        public bool HasErrors => _errors.Count > 0;

        /// <inheritdoc cref="ExecutionOptions.Inputs"/>
        public Inputs Inputs { get; set; }

        /// <summary>
        /// Adds a validation error to the list of validation errors.
        /// </summary>
        public void ReportError(ValidationError error)
        {
            _errors.Add(error ?? throw new ArgumentNullException(nameof(error), "Must provide a validation error."));
        }

        /// <summary>
        /// For a node with a selection set, returns a list of variable references along with what input type each were referenced for.
        /// </summary>
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

        /// <summary>
        /// For a specified operation with a document, returns a list of variable references
        /// along with what input type each was referenced for.
        /// </summary>
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

        /// <summary>
        /// Searches the document for a fragment definition by name and returns it.
        /// </summary>
        public FragmentDefinition GetFragment(string name)
        {
            return Document.Fragments.FindDefinition(name);
        }

        /// <summary>
        /// Returns a list of fragment spreads within the specified node.
        /// </summary>
        public List<FragmentSpread> GetFragmentSpreads(SelectionSet node)
        {
            var spreads = new List<FragmentSpread>();

            var setsToVisit = new Stack<SelectionSet>(new[] { node });

            while (setsToVisit.Count > 0)
            {
                var set = setsToVisit.Pop();

                foreach (var selection in set.SelectionsList)
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

        /// <summary>
        /// For a specified operation within a document, returns a list of all fragment definitions in use.
        /// </summary>
        public IEnumerable<FragmentDefinition> GetRecursivelyReferencedFragments(Operation operation)
        {
            if (_fragments.TryGetValue(operation, out var results))
            {
                return results;
            }

            var fragments = new List<FragmentDefinition>();
            var nodesToVisit = new Stack<SelectionSet>(new[] { operation.SelectionSet });
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

        /// <summary>
        /// Returns a string representation of the specified node.
        /// </summary>
        public string Print(INode node)
        {
            return AstPrinter.Print(node);
        }

        /// <summary>
        /// Returns the name of the specified graph type.
        /// </summary>
        public string Print(IGraphType type)
        {
            return SchemaPrinter.ResolveName(type);
        }
    }

    /// <summary>
    /// Represents a variable reference node and the graph type it is referenced to be used for.
    /// </summary>
    public class VariableUsage
    {
        /// <summary>
        /// Returns a variable reference node.
        /// </summary>
        public VariableReference Node { get; }
        /// <summary>
        /// Returns a graph type.
        /// </summary>
        public IGraphType Type { get; }

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="node">A variable reference node.</param>
        /// <param name="type">A graph type.</param>
        public VariableUsage(VariableReference node, IGraphType type)
        {
            Node = node;
            Type = type;
        }
    }
}
