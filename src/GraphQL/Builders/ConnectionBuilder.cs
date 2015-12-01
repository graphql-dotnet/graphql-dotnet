using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Builders
{
    public static class ConnectionBuilder
    {
        public static ConnectionBuilder<TParentType, TGraphType, object> Create<TParentType, TGraphType>()
            where TParentType : GraphType
            where TGraphType : ObjectGraphType, new()
        {
            return ConnectionBuilder<TParentType, TGraphType, object>.Create();
        }
    }

    public class ConnectionBuilder<TParentType, TGraphType, TObjectType>
        where TParentType : GraphType
        where TGraphType : ObjectGraphType, new()
    {
        public class ResolutionArguments : FieldBuilder<TGraphType, TObjectType, IEnumerable<TGraphType>>.ResolutionArguments
        {
            private readonly int? _defaultPageSize;

            public ResolutionArguments(ResolveFieldContext context, Func<object, TObjectType> objectResolver, bool isUnidirectional, int? defaultPageSize)
                : base(context, objectResolver)
            {
                IsUnidirectional = isUnidirectional;
                _defaultPageSize = defaultPageSize;
            }

            public bool IsUnidirectional { get; set; }

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

        public static ConnectionBuilder<TParentType, TGraphType, TObjectType> Create()
        {
            var fieldType = new FieldType
            {
                Type = typeof(ConnectionType<TParentType, TGraphType>),
                Arguments = new QueryArguments(new QueryArgument[0]),
            };
            return new ConnectionBuilder<TParentType, TGraphType, TObjectType>(fieldType, null, false, false, null)
                .Unidirectional();
        }

        public ConnectionBuilder<TParentType, TGraphType, TObjectType> Unidirectional()
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

        public ConnectionBuilder<TParentType, TGraphType, TObjectType> Bidirectional()
        {
            if (_isBidirectional)
            {
                return this;
            }

            Argument<StringGraphType, string>("before",
                "Only look at connected edges with cursors smaller than the value of `before`.");
            Argument<IntGraphType, int?>("last",
                "Specifies the number of edges to return counting reversly from `before` or the last entry if `before` is not specified");

            _isUnidirectional = false;
            _isBidirectional = true;

            return this;
        }

        public ConnectionBuilder<TParentType, TGraphType, TObjectType> Name(string name)
        {
            FieldType.Name = name;
            return this;
        }

        public ConnectionBuilder<TParentType, TGraphType, TObjectType> Description(string description)
        {
            FieldType.Description = description;
            return this;
        }

        public ConnectionBuilder<TParentType, TGraphType, TObjectType> PageSize(int pageSize)
        {
            _pageSize = pageSize;
            return this;
        }

        public ConnectionBuilder<TParentType, TGraphType, TObjectType> ReturnAll()
        {
            _pageSize = null;
            return this;
        }

        public ConnectionBuilder<TParentType, TGraphType, TNewObjectType> WithObject<TNewObjectType>(Func<object, TNewObjectType> objectResolver = null)
        {
            return new ConnectionBuilder<TParentType, TGraphType, TNewObjectType>(
                FieldType,
                objectResolver ?? (obj => (TNewObjectType)obj),
                _isUnidirectional,
                _isBidirectional,
                _pageSize);
        }

        public ConnectionBuilder<TParentType, TGraphType, TObjectType> Argument<TArgumentGraphType>(string name, string description)
        {
            FieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
            });
            return this;
        }

        public ConnectionBuilder<TParentType, TGraphType, TObjectType> Argument<TArgumentGraphType, TArgumentType>(string name, string description,
            TArgumentType defaultValue = default(TArgumentType))
        {
            FieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
                DefaultValue = defaultValue,
            });
            return this;
        }

        public void Resolve(Func<ResolutionArguments, IEnumerable<TGraphType>> resolver)
        {
            FieldType.Resolve = context =>
            {
                var args = new ResolutionArguments(context, _objectResolver, _isUnidirectional, _pageSize);
                CheckForErrors(args);
                var entries = resolver(args);
                return ResolveCallback(args, entries != null ? entries.ToList() : new List<TGraphType>());
            };
        }

        public void Resolve(Func<ResolutionArguments, Task<IEnumerable<TGraphType>>> resolver)
        {
            FieldType.Resolve = context =>
            {
                var args = new ResolutionArguments(context, _objectResolver, _isUnidirectional, _pageSize);
                CheckForErrors(args);
                var entries = resolver(args).Result;
                return ResolveCallback(args, entries != null ? entries.ToList() : new List<TGraphType>());
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

        private ConnectionType<TParentType, TGraphType> ResolveCallback(ResolutionArguments args, List<TGraphType> entries)
        {
            var totalCount = args.IsPartial ? args.TotalCount : entries.Count;
            var limit = args.PageSize ?? totalCount ?? 25;
            var firstIndex = args.NumberOfSkippedEntries ?? 0;
            var skip = 0;

            if (!args.IsUnidirectional && !totalCount.HasValue)
            {
                throw new ArgumentException("`totalCount` is not set for partial bidirectional connection.");
            }

            var after = ParseCursor(args.After);
            var before = ParseCursor(args.Before);

            if (!args.IsPartial)
            {
                if (after.HasValue)
                {
                    skip = Math.Max(after.Value - firstIndex, 0);
                }
                else if (before.HasValue)
                {
                    var count = totalCount.GetValueOrDefault(0);
                    var skipFromEnd = Math.Max(count - before.Value + 1, 0);
                    skip = count - limit - skipFromEnd;
                    if (skip < 0)
                    {
                        skip = 0;
                        limit = 0;
                    }
                }
                firstIndex = skip;
            }

            var cursor = firstIndex;
            var edges = entries
                .Skip(skip)
                .Take(limit)
                .Select(entry =>
                    new EdgeType<TParentType, TGraphType>
                    {
                        Cursor = GetCursorFromIndex(++cursor),
                        Node = entry,
                    })
                .ToList();
            var takeCount = edges.Count;

            var pageInfo = new PageInfoType
            {
                HasNextPage = takeCount < entries.Count - skip,
                HasPreviousPage = firstIndex > 0,
                StartCursor = GetCursorFromIndex(firstIndex + 1),
                EndCursor = GetCursorFromIndex(firstIndex + Math.Max(1, takeCount)),
            };

            return new ConnectionType<TParentType, TGraphType>
            {
                TotalCount = totalCount,
                Edges = edges,
                PageInfo = pageInfo,
            };
        }

        private static string GetCursorFromIndex(int index)
        {
            return index.ToString("D8");
        }

        private static int? ParseCursor(string cursor)
        {
            int cursorInt;
            if (cursor != null && int.TryParse(cursor, out cursorInt))
            {
                return cursorInt;
            }
            return null;
        }
    }
}
