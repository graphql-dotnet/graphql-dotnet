using System.Diagnostics;
using GraphQL.Execution;
using GraphQL.Resolvers;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents a field of a graph type.
    /// </summary>
    [DebuggerDisplay("{Name,nq}: {ResolvedType,nq}")]
    public class FieldType : MetadataProvider, IFieldType
    {
        private string? _name;
        /// <inheritdoc/>
        public string Name
        {
            get => _name!;
            set => SetName(value, validate: true);
        }

        internal void SetName(string name, bool validate)
        {
            if (_name != name)
            {
                if (validate)
                {
                    NameValidator.ValidateName(name, NamedElement.Field);
                }

                _name = name;
            }
        }

        /// <inheritdoc/>
        public string? Description { get; set; }

        /// <inheritdoc/>
        public string? DeprecationReason
        {
            get => this.GetDeprecationReason();
            set => this.SetDeprecationReason(value);
        }

        /// <summary>
        /// Gets or sets the default value of the field. Only applies to fields of input object graph types.
        /// </summary>
        public object? DefaultValue { get; set; }

        private Type? _type;
        /// <summary>
        /// Gets or sets the graph type of this field.
        /// </summary>
        public Type? Type
        {
            get => _type;
            set
            {
                if (value != null && !value.IsGraphType())
                    throw new ArgumentOutOfRangeException(nameof(value), $"Type '{value}' is not a graph type.");
                if (value != null && value.IsGenericTypeDefinition)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Type '{value}' should not be an open generic type definition.");
                _type = value;
            }
        }

        /// <summary>
        /// Gets or sets the graph type of this field.
        /// </summary>
        public IGraphType? ResolvedType { get; set; }

        /// <inheritdoc/>
        public QueryArguments? Arguments { get; set; }

        /// <summary>
        /// This property contains the argument values supplied to the field resolver if no arguments
        /// to the field were supplied within the request. This property serves as an optimization in
        /// <see cref="ReadonlyResolveFieldContext.Arguments"/>. So basically, we are optimizing for
        /// the idea that much of the time there are no field arguments specified, and simply the
        /// default set needs to be returned. 
        /// Note that this value is automatically initialized during schema initialization.
        /// </summary>
        internal IDictionary<string, ArgumentValue>? DefaultArgumentValues { get; set; }

        /// <summary>
        /// Gets or sets a field resolver for the field. Only applicable to fields of output graph types.
        /// </summary>
        public IFieldResolver? Resolver { get; set; }

        /// <summary>
        /// Gets or sets a subscription resolver for the field. Only applicable to the root fields of subscription.
        /// </summary>
        public ISourceStreamResolver? StreamResolver { get; set; }
    }
}
