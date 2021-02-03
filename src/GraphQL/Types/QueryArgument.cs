using System;
using System.Diagnostics;
using GraphQL.Language.AST;
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
    public class QueryArgument : MetadataProvider, IHaveDefaultValue, IProvideDescription
    {
        private Type _type;
        private IGraphType _resolvedType;
        private object _defaultValue;
        private IValue _defaultValueAST;

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

        private string _name;
        /// <summary>
        /// Gets or sets the name of the argument.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    NameValidator.ValidateName(value, "argument");
                    _name = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the description of the argument.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the default value of the argument.
        /// </summary>
        public object DefaultValue
        {
            get => _defaultValue;
            set
            {
                if (!(ResolvedType?.GetNamedType() is GraphQLTypeReference))
                    _ = value.AstFromValue(null, ResolvedType); // HACK: https://github.com/graphql-dotnet/graphql-dotnet/issues/1795

                _defaultValue = value;
                _defaultValueAST = null;
            }
        }

        /// <inheritdoc/>
        public IGraphType ResolvedType
        {
            get => _resolvedType;
            set => _resolvedType = CheckResolvedType(value);
        }

        /// <inheritdoc/>
        public Type Type
        {
            get => _type;
            private set => _type = CheckType(value);
        }

        private Type CheckType(Type type)
        {
            if (type?.IsInputType() == false)
                throw Create(nameof(Type), type);

            return type;
        }

        private IGraphType CheckResolvedType(IGraphType type)
        {
            if (!(type.GetNamedType() is GraphQLTypeReference) && type?.IsInputType() == false)
                throw Create(nameof(ResolvedType), type.GetType());

            return type;
        }

        private ArgumentOutOfRangeException Create(string paramName, Type value) => new ArgumentOutOfRangeException(paramName,
            $"'{value.GetFriendlyName()}' is not a valid input type. QueryArgument must be one of the input types: ScalarGraphType, EnumerationGraphType or IInputObjectGraphType.");

        internal IValue GetDefaultValueAST(ISchema schema)
        {
            if (_defaultValueAST == null && _defaultValue != null)
                _defaultValueAST = _defaultValue.AstFromValue(schema, ResolvedType);

            return _defaultValueAST;
        }
    }
}
