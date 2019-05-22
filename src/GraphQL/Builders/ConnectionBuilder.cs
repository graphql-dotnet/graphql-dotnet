using System;
using System.Threading.Tasks;
using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Utilities;

namespace GraphQL.Builders
{
    public static class ConnectionBuilder
    {
        public static ConnectionBuilder<TSourceType> Create<TNodeType, TSourceType>()
            where TNodeType : IGraphType
        {
            return ConnectionBuilder<TSourceType>.Create<TNodeType>();
        }

        public static ConnectionBuilder<TSourceType> Create<TNodeType, TEdgeType, TSourceType>()
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
        {
            return ConnectionBuilder<TSourceType>.Create<TNodeType, TEdgeType>();
        }

        public static ConnectionBuilder<TSourceType> Create<TNodeType, TEdgeType, TConnectionType, TSourceType>()
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
            where TConnectionType : ConnectionType<TNodeType, TEdgeType>
        {
            return ConnectionBuilder<TSourceType>.Create<TNodeType, TEdgeType, TConnectionType>();
        }
    }

    public class ConnectionBuilder<TSourceType>
    {
        private bool _isUnidirectional;

        private bool _isBidirectional;

        private int? _pageSize;

        public FieldType FieldType { get; protected set; }

        private ConnectionBuilder(
            FieldType fieldType,
            bool isUnidirectional,
            bool isBidirectional,
            int? pageSize)
        {
            _isUnidirectional = isUnidirectional;
            _isBidirectional = isBidirectional;
            _pageSize = pageSize;
            FieldType = fieldType;
        }

        public static ConnectionBuilder<TSourceType> Create<TNodeType>(string name = "default")
            where TNodeType : IGraphType
        {
            return Create<TNodeType, EdgeType<TNodeType>>(name);
        }

        public static ConnectionBuilder<TSourceType> Create<TNodeType, TEdgeType>(string name = "default")
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
        {
            return Create<TNodeType, TEdgeType, ConnectionType<TNodeType, TEdgeType>>(name);
        }

        public static ConnectionBuilder<TSourceType> Create<TNodeType, TEdgeType, TConnectionType>(string name = "default")
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
            where TConnectionType : ConnectionType<TNodeType, TEdgeType>
        {
            var fieldType = new FieldType
            {
                Name = name,
                Type = typeof(TConnectionType),
                Arguments = new QueryArguments(new QueryArgument[0]),
            };
            return new ConnectionBuilder<TSourceType>(fieldType, false, false, null)
                .Unidirectional();
        }

        public ConnectionBuilder<TSourceType> Unidirectional()
        {
            if (_isUnidirectional)
            {
                return this;
            }

            Argument<StringGraphType, string>("after",
                "Only look at connected edges with cursors greater than the value of `after`.");
            Argument<IntGraphType, int?>("first",
                "Specifies the number of edges to return starting from `after` or the first entry if `after` is not specified.");

            _isUnidirectional = true;
            _isBidirectional = false;

            return this;
        }

        public ConnectionBuilder<TSourceType> Bidirectional()
        {
            if (_isBidirectional)
            {
                return this;
            }

            Argument<StringGraphType, string>("before",
                "Only look at connected edges with cursors smaller than the value of `before`.");
            Argument<IntGraphType, int?>("last",
                "Specifies the number of edges to return counting reversely from `before`, or the last entry if `before` is not specified.");

            _isUnidirectional = false;
            _isBidirectional = true;

            return this;
        }

        public ConnectionBuilder<TSourceType> Name(string name)
        {
            NameValidator.ValidateName(name);

            FieldType.Name = name;
            return this;
        }

        public ConnectionBuilder<TSourceType> Description(string description)
        {
            FieldType.Description = description;
            return this;
        }

        public ConnectionBuilder<TSourceType> DeprecationReason(string deprecationReason)
        {
            FieldType.DeprecationReason = deprecationReason;
            return this;
        }

        public ConnectionBuilder<TSourceType> PageSize(int pageSize)
        {
            _pageSize = pageSize;
            return this;
        }

        public ConnectionBuilder<TSourceType> ReturnAll()
        {
            _pageSize = null;
            return this;
        }

        public ConnectionBuilder<TSourceType> Argument<TArgumentGraphType>(string name, string description)
            where TArgumentGraphType : IGraphType
        {
            FieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
            });
            return this;
        }

        public ConnectionBuilder<TSourceType> Argument<TArgumentGraphType, TArgumentType>(string name, string description,
            TArgumentType defaultValue = default)
            where TArgumentGraphType : IGraphType
        {
            FieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
                DefaultValue = defaultValue,
            });
            return this;
        }

        public void Resolve(Func<ResolveConnectionContext<TSourceType>, object> resolver)
        {
            FieldType.Resolver = new Resolvers.FuncFieldResolver<object>(context =>
            {
                var args = new ResolveConnectionContext<TSourceType>(context, _isUnidirectional, _pageSize);
                CheckForErrors(args);
                return resolver(args);
            });
        }

        public void ResolveAsync(Func<ResolveConnectionContext<TSourceType>, Task<object>> resolver)
        {
            FieldType.Resolver = new Resolvers.AsyncFieldResolver<object>(context =>
            {
                var args = new ResolveConnectionContext<TSourceType>(context, _isUnidirectional, _pageSize);
                CheckForErrors(args);
                return resolver(args);
            });
        }

        private void CheckForErrors(ResolveConnectionContext<TSourceType> args)
        {
            if (args.First.HasValue && args.Last.HasValue)
            {
                throw new ArgumentException("Cannot specify both `first` and `last`.");
            }
            if (args.IsUnidirectional && args.Last.HasValue)
            {
                throw new ArgumentException("Cannot use `last` with unidirectional connections.");
            }
            if (args.IsPartial && args.NumberOfSkippedEntries.HasValue)
            {
                throw new ArgumentException("Cannot specify `numberOfSkippedEntries` with partial connection resolvers.");
            }
        }
    }
}
