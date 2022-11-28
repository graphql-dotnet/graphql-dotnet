using System.Collections;
using GraphQL.Execution;
using GraphQLParser;

namespace GraphQL.Validation
{
    /// <summary>
    /// Contains a list of variables (name &amp; value tuples) that have been gathered from the document and attached <see cref="Inputs"/>.
    /// </summary>
    public class Variables : IEnumerable<Variable>
    {
        private List<Variable>? _variables;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public Variables()
        {
        }

        internal Variables(int initialCount)
        {
            _variables = new List<Variable>(initialCount);
        }

        /// <summary>
        /// Adds a variable to the list.
        /// </summary>
        public virtual void Add(Variable variable) => (_variables ??= new()).Add(variable ?? throw new ArgumentNullException(nameof(variable)));

        /// <summary>
        /// Returns the first variable with a matching name, or <paramref name="defaultValue"/> if none are found.
        /// </summary>
        public object? ValueFor(string name, object? defaultValue = null)
        {
            return ValueFor(name, out var value) ? value.Value : defaultValue;
        }

        /// <summary>
        /// Gets the first variable with a matching name. Returns <see langword="true"/> if a match is found.
        /// </summary>
        public bool ValueFor(ROM name, out ArgumentValue value)
        {
            // DO NOT USE LINQ ON HOT PATH
            if (_variables != null)
            {
                foreach (var v in _variables)
                {
                    if (v.Name == name)
                    {
                        value = new ArgumentValue(v.Value, v.IsDefault || !v.ValueSpecified ? ArgumentSource.VariableDefault : ArgumentSource.Variable);
                        return v.ValueSpecified;
                    }
                }
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public IEnumerator<Variable> GetEnumerator() => (_variables ?? Enumerable.Empty<Variable>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Returns a static instance that holds no variables.
        /// </summary>
        public static Variables None { get; } = new NoVariables();

        private sealed class NoVariables : Variables
        {
            public NoVariables() : base() { }
            public override void Add(Variable variable) => throw new InvalidOperationException("Cannot add variables to this instance.");
        }
    }

}
