using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Validation
{
    public class ValidationContext : IProvideUserContext
    {
        private List<ValidationError> _errors;

        private readonly Dictionary<Operation, IEnumerable<FragmentDefinition>> _fragments
            = new Dictionary<Operation, IEnumerable<FragmentDefinition>>();

        private readonly Dictionary<Operation, IEnumerable<VariableUsage>> _variables =
            new Dictionary<Operation, IEnumerable<VariableUsage>>();

        /// <summary>
        /// Allows validation rules store their specific data during validation.
        /// </summary>

        private readonly ConcurrentDictionary<object, object> _validationRuleLocalContext = new ConcurrentDictionary<object, object>();

        public string OriginalQuery { get; set; }

        public string OperationName { get; set; }

        public ISchema Schema { get; set; }

        public Document Document { get; set; }

        public TypeInfo TypeInfo { get; set; }

        public IDictionary<string, object> UserContext { get; set; }

        /// <summary>
        /// Gets some data specific to validation rule from validation context.
        /// </summary>
        /// <typeparam name="TValidationRule">The type of validation rule.</typeparam>
        /// <typeparam name="TResult">Type of data.</typeparam>
        /// <returns>Previously stored data if any, otherwise throws <see cref="InvalidOperationException"/>.</returns>
        internal TResult Get<TValidationRule, TResult>() => (TResult)_validationRuleLocalContext[typeof(TValidationRule)] ?? throw new InvalidOperationException("No data");

        /// <summary>
        /// Sets some data specific to validation rule into validation context.
        /// </summary>
        /// <typeparam name="TValidationRule">The type of validation rule.</typeparam>
        /// <param name="data">Arbitrary object used by the specified rule during validation.</param>
        internal void Set<TValidationRule>(object data)
            where TValidationRule : IValidationRule
            => _validationRuleLocalContext[typeof(TValidationRule)] = data;

        public IEnumerable<ValidationError> Errors => (IEnumerable<ValidationError>)_errors ?? Array.Empty<ValidationError>();

        public bool HasErrors => _errors?.Count > 0;

        public Inputs Inputs { get; set; }

        public void ReportError(ValidationError error)
        {
            if (error == null)
                throw new ArgumentNullException(nameof(error), "Must provide a validation error.");
            (_errors ??= new List<ValidationError>()).Add(error);
        }

        public List<VariableUsage> GetVariables(IHaveSelectionSet node)
        {
            var usages = new List<VariableUsage>();
            var info = new TypeInfo(Schema);

            var listener = new EnterLeaveListener(_ =>
            {
                _.Match<VariableReference>(
                    (varRef, context) => usages.Add(new VariableUsage(varRef, info.GetInputType()))
                );
            });

            var visitor = new BasicVisitor(info, listener);
            visitor.Visit(node, this);

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
