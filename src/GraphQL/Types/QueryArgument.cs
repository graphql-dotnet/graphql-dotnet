using System;
using System.Diagnostics;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    public interface IQueryArgument : IProvideMetadata, IHaveDefaultValue, INamedType
    {
    }

    public class QueryArgument<TType> : QueryArgument
        where TType : IGraphType
    {
        public QueryArgument()
            : base(typeof(TType))
        {
        }
    }

    [DebuggerDisplay("{Name,nq}: {ResolvedType,nq}")]
    public class QueryArgument : MetadataProvider, IHaveDefaultValue, IQueryArgument
    {
        private Type _type;
        private IGraphType _resolvedType;

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

        public object DefaultValue { get; set; }

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
    }
}
