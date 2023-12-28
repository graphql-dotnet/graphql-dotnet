using System.Diagnostics;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents an argument to a field or directive.
    /// </summary>
    /// <typeparam name="TType">The graph type of the argument.</typeparam>
    public class QueryArgument<TType> : QueryArgument
        where TType : IGraphType
    {
        /// <summary>
        /// Initializes a new instance of the argument.
        /// </summary>
        public QueryArgument()
            : base(typeof(TType))
        {
        }
    }

    /// <summary>
    /// Represents an argument to a field or directive.
    /// </summary>
    [DebuggerDisplay("{Name,nq}: {ResolvedType,nq}")]
    public class QueryArgument : MetadataProvider, IHaveDefaultValue, IProvideDescription, IProvideDeprecationReason
    {
        private Type? _type;
        private IGraphType? _resolvedType;

        /// <summary>
        /// Initializes a new instance of the argument.
        /// </summary>
        /// <param name="type">The graph type of the argument.</param>
        public QueryArgument(IGraphType type)
        {
            ResolvedType = type ?? throw new ArgumentOutOfRangeException(nameof(type), "QueryArgument type is required");
        }

        /// <summary>
        /// Initializes a new instance of the argument.
        /// </summary>
        /// <param name="type">The graph type of the argument.</param>
        public QueryArgument(Type type)
        {
            if (type == null || !typeof(IGraphType).IsAssignableFrom(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "QueryArgument type is required and must derive from IGraphType.");
            }

            Type = type;
        }

        private string? _name;
        /// <summary>
        /// Gets or sets the name of the argument.
        /// </summary>
        public string Name
        {
            get => _name!;
            set
            {
                if (_name != value)
                {
                    NameValidator.ValidateName(value, NamedElement.Argument);
                    _name = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the description of the argument.
        /// </summary>
        public string? Description { get; set; }

        /// <inheritdoc/>
        public string? DeprecationReason
        {
            get => this.GetDeprecationReason();
            set => this.SetDeprecationReason(value);
        }

        /// <summary>
        /// Gets or sets the default value of the argument.
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Returns the graph type of this argument.
        /// </summary>
        public IGraphType? ResolvedType
        {
            get => _resolvedType;
            set => _resolvedType = CheckResolvedType(value);
        }

        /// <inheritdoc/>
        public Type? Type
        {
            get => _type;
            internal set => _type = CheckType(value);
        }

        private Type? CheckType(Type? type)
        {
            if (type?.IsInputType() == false)
                throw Create(nameof(Type), type);

            return type;
        }

        private IGraphType? CheckResolvedType(IGraphType? type)
        {
            if (type != null && !type.IsGraphQLTypeReference() && !type.IsInputType())
                throw Create(nameof(ResolvedType), type.GetType());

            return type;
        }

        /// <summary>
        /// Parses the value received from the client when the value is not <see langword="null"/>.
        /// Occurs during validation prior to <see cref="Validator"/>.
        /// Throw an exception if necessary to indicate a problem.
        /// Only applicable to fields of input graph types.
        /// </summary>
        public Func<object, object> Parser { get; set; } = DefaultParser;

        /// <summary>
        /// Default parsing method that returns the value as-is.
        /// </summary>
        internal static readonly Func<object, object> DefaultParser = static value => value;

        /// <summary>
        /// Validates the value received from the client when the value is not <see langword="null"/>.
        /// Occurs during validation after <see cref="Parser"/> has parsed the value.
        /// Throw an exception if necessary to indicate a problem.
        /// Only applicable to fields of input graph types.
        /// </summary>
        public Action<object> Validator { get; set; } = DefaultValidator;

        /// <summary>
        /// Default validation method that does nothing.
        /// </summary>
        internal static readonly Action<object> DefaultValidator = static _ => { };

        private ArgumentOutOfRangeException Create(string paramName, Type value) => new(paramName,
            $"'{value.GetFriendlyName()}' is not a valid input type. QueryArgument must be one of the input types: ScalarGraphType, EnumerationGraphType or IInputObjectGraphType.");
    }
}
