using System;
using GraphQL.Types;
using GraphQL.Types.Relay;

namespace GraphQL.Builders
{
    public static class ConnectionBuilder
    {
        public static ConnectionBuilder<TGraphType, object> Create<TGraphType>()
            where TGraphType : ObjectGraphType
        {
            return ConnectionBuilder<TGraphType, object>.Create();
        }
    }

    public class ConnectionBuilder<TGraphType, TObjectType>
        where TGraphType : ObjectGraphType
    {
        public class ResolutionArguments : FieldBuilder<TGraphType, TObjectType, object>.ResolutionArguments
        {
            private readonly int? _defaultPageSize;

            public ResolutionArguments(ResolveFieldContext context, Func<object, TObjectType> objectResolver, bool isUnidirectional, int? defaultPageSize)
                : base(context, objectResolver)
            {
                IsUnidirectional = isUnidirectional;
                _defaultPageSize = defaultPageSize;
            }

            public bool IsUnidirectional { get; private set; }

            public int? First
            {
                get
                {
                    var first = GetArgument<int?>("first");
                    return first.HasValue ? (int?)Math.Abs(first.Value) : null;
                }
            }

            public int? Last
            {
                get
                {
                    var last = GetArgument<int?>("last");
                    return last.HasValue ? (int?)Math.Abs(last.Value) : null;
                }
            }

            public string After
            {
                get { return GetArgument<string>("after"); }
            }

            public string Before
            {
                get { return GetArgument<string>("before"); }
            }

            public int? PageSize
            {
                get { return First ?? Last ?? _defaultPageSize; }
            }

            public int? NumberOfSkippedEntries { get; set; }

            public int? TotalCount { get; set; }

            public bool IsPartial { get; set; }
        }

        private readonly Func<object, TObjectType> _objectResolver;

        private bool _isUnidirectional;

        private bool _isBidirectional;

        private int? _pageSize;

        public FieldType FieldType { get; protected set; }

        private ConnectionBuilder(
            FieldType fieldType,
            Func<object, TObjectType> objectResolver,
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

        public static ConnectionBuilder<TGraphType, TObjectType> Create()
        {
            var fieldType = new FieldType
            {
                Type = typeof(ConnectionType<TGraphType>),
                Arguments = new QueryArguments(new QueryArgument[0]),
            };
            return new ConnectionBuilder<TGraphType, TObjectType>(fieldType, null, false, false, null)
                .Unidirectional();
        }

        public ConnectionBuilder<TGraphType, TObjectType> Unidirectional()
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

        public ConnectionBuilder<TGraphType, TObjectType> Bidirectional()
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

        public ConnectionBuilder<TGraphType, TObjectType> Name(string name)
        {
            FieldType.Name = name;
            return this;
        }

        public ConnectionBuilder<TGraphType, TObjectType> Description(string description)
        {
            FieldType.Description = description;
            return this;
        }

        public ConnectionBuilder<TGraphType, TObjectType> DeprecationReason(string deprecationReason)
        {
            FieldType.DeprecationReason = deprecationReason;
            return this;
        }

        public ConnectionBuilder<TGraphType, TObjectType> PageSize(int pageSize)
        {
            _pageSize = pageSize;
            return this;
        }

        public ConnectionBuilder<TGraphType, TObjectType> ReturnAll()
        {
            _pageSize = null;
            return this;
        }

        public ConnectionBuilder<TGraphType, TNewObjectType> WithObject<TNewObjectType>(Func<object, TNewObjectType> objectResolver = null)
        {
            return new ConnectionBuilder<TGraphType, TNewObjectType>(
                FieldType,
                objectResolver ?? (obj => (TNewObjectType)obj),
                _isUnidirectional,
                _isBidirectional,
                _pageSize);
        }

        public ConnectionBuilder<TGraphType, TObjectType> Argument<TArgumentGraphType>(string name, string description)
            where TArgumentGraphType : GraphType
        {
            FieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
            });
            return this;
        }

        public ConnectionBuilder<TGraphType, TObjectType> Argument<TArgumentGraphType, TArgumentType>(string name, string description,
            TArgumentType defaultValue = default(TArgumentType))
            where TArgumentGraphType : GraphType
        {
            FieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
                DefaultValue = defaultValue,
            });
            return this;
        }

        public void Resolve(Func<ResolutionArguments, object> resolver)
        {
            FieldType.Resolve = context =>
            {
                var args = new ResolutionArguments(context, _objectResolver, _isUnidirectional, _pageSize);
                CheckForErrors(args);
                return resolver(args);
            };
        }

        private void CheckForErrors(ResolutionArguments args)
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
