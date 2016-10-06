using System;
using GraphQL.Types;
using GraphQL.Types.Relay;

namespace GraphQL.Builders
{
    public static class ConnectionBuilder
    {
        public static ConnectionBuilder<TGraphType, TSourceType> Create<TGraphType, TSourceType>()
            where TGraphType : IGraphType
        {
            return ConnectionBuilder<TGraphType, TSourceType>.Create();
        }
    }

    public class ConnectionBuilder<TGraphType, TSourceType>
        where TGraphType : IGraphType
    {

        private readonly Func<object, TSourceType> _objectResolver;

        private bool _isUnidirectional;

        private bool _isBidirectional;

        private int? _pageSize;

        public FieldType FieldType { get; protected set; }

        private ConnectionBuilder(
            FieldType fieldType,
            Func<object, TSourceType> objectResolver,
            bool isUnidirectional,
            bool isBidirectional,
            int? pageSize)
        {
            _objectResolver = objectResolver;
            _isUnidirectional = isUnidirectional;
            _isBidirectional = isBidirectional;
            _pageSize = pageSize;
            FieldType = fieldType;
        }

        public static ConnectionBuilder<TGraphType, TSourceType> Create()
        {
            var fieldType = new FieldType
            {
                Type = typeof(ConnectionType<TGraphType>),
                Arguments = new QueryArguments(new QueryArgument[0]),
            };
            return new ConnectionBuilder<TGraphType, TSourceType>(fieldType, null, false, false, null)
                .Unidirectional();
        }

        public ConnectionBuilder<TGraphType, TSourceType> Unidirectional()
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

        public ConnectionBuilder<TGraphType, TSourceType> Bidirectional()
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

        public ConnectionBuilder<TGraphType, TSourceType> Name(string name)
        {
            FieldType.Name = name;
            return this;
        }

        public ConnectionBuilder<TGraphType, TSourceType> Description(string description)
        {
            FieldType.Description = description;
            return this;
        }

        public ConnectionBuilder<TGraphType, TSourceType> DeprecationReason(string deprecationReason)
        {
            FieldType.DeprecationReason = deprecationReason;
            return this;
        }

        public ConnectionBuilder<TGraphType, TSourceType> PageSize(int pageSize)
        {
            _pageSize = pageSize;
            return this;
        }

        public ConnectionBuilder<TGraphType, TSourceType> ReturnAll()
        {
            _pageSize = null;
            return this;
        }

        public ConnectionBuilder<TGraphType, TSourceType> Argument<TArgumentGraphType>(string name, string description)
            where TArgumentGraphType : IGraphType
        {
            FieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
            });
            return this;
        }

        public ConnectionBuilder<TGraphType, TSourceType> Argument<TArgumentGraphType, TArgumentType>(string name, string description,
            TArgumentType defaultValue = default(TArgumentType))
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

        private void CheckForErrors(ResolveConnectionContext<TSourceType> args)
        {
            if (args.First.HasValue && args.Before != null)
            {
                throw new ArgumentException("Cannot specify both `first` and `before`.");
            }
            if (args.Last.HasValue && args.After != null)
            {
                throw new ArgumentException("Cannot specify both `last` and `after`.");
            }
            if (args.Before != null && args.After != null)
            {
                throw new ArgumentException("Cannot specify both `before` and `after`.");
            }
            if (args.First.HasValue && args.Last.HasValue)
            {
                throw new ArgumentException("Cannot specify both `first` and `last`.");
            }
            if (args.IsUnidirectional && (args.Last.HasValue || args.Before != null))
            {
                throw new ArgumentException("Cannot use `last` and `before` with unidirectional connections.");
            }
            if (args.IsPartial && args.NumberOfSkippedEntries.HasValue)
            {
                throw new ArgumentException("Cannot specify `numberOfSkippedEntries` with partial connection resolvers.");
            }
        }
    }
}
