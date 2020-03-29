using System;
using System.Diagnostics;
using GraphQL.Language.AST;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    public class QueryArgument<TType> : QueryArgument
        where TType : IGraphType
    {
        public QueryArgument()
            : base(typeof(TType))
        {
        }
    }

    [DebuggerDisplay("{Name,nq}: {ResolvedType,nq}")]
    public class QueryArgument : MetadataProvider, IHaveDefaultValue
    {
        private Type _type;
        private IGraphType _resolvedType;
        private object _defaultValue;
        private IValue _defaultValueAST;

        public QueryArgument(IGraphType type)
        {
            ResolvedType = type ?? throw new ArgumentOutOfRangeException(nameof(type), "QueryArgument type is required");
        }

        public QueryArgument(Type type)
        {
            if (type == null || !typeof(IGraphType).IsAssignableFrom(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "QueryArgument type is required and must derive from IGraphType.");
            }

            Type = type;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public object DefaultValue
        {
            get => _defaultValue;
            set
            {
                _defaultValue = value;
                _defaultValueAST = null;
            }
        }

        public IGraphType ResolvedType
        {
            get => _resolvedType;
            set => _resolvedType = CheckResolvedType(value);
        }

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
