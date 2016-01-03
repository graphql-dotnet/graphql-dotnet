using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GraphQL.Types
{
    public class ConnectionType<TTo> : ObjectGraphType
        where TTo : ObjectGraphType, new()
    {
        public ConnectionType()
        {
            Name = string.Format("{0}Connection", typeof(TTo).GraphQLName());
            Description = string.Format("A connection from an object to a list of objects of type `{0}`.",
                typeof(TTo).GraphQLName());

            Field<IntGraphType>()
                .Name("totalCount")
                .Description(
                    "A count of the total number of objects in this connection, ignoring pagination. " +
                    "This allows a client to fetch the first five objects by passing \"5\" as the argument " +
                    "to `first`, then fetch the total count so it could display \"5 of 83\", for example. " +
                    "In cases where we employ infinite scrolling or don't have an exact count of entries, " +
                    "this field will return `null`.");

            Field<NonNullGraphType<PageInfoType>>()
                .Name("pageInfo")
                .Description("Information to aid in pagination.");

            Field<ListGraphType<EdgeType<TTo>>>()
                .Name("edges")
                .Description("Information to aid in pagination.");

            Field<ListGraphType<TTo>>()
                .Name("items")
                .Description(
                    "A list of all of the objects returned in the connection. This is a convenience field provided " +
                    "for quickly exploring the API; rather than querying for \"{ edges { node } }\" when no edge data " +
                    "is needed, this field can be used instead. Note that when clients like Relay need to fetch " +
                    "the \"cursor\" field on the edge to enable efficient pagination, this shortcut cannot be used, " +
                    "and the full \"{ edges { node } } \" version should be used instead.");
        }
    }

    public class Connection<T>
    {
        private IEnumerable<T> _collection;
        private PageInfo _pageInfo;
        private List<Edge<T>> _edges;
        private int? _first;
        private int? _last;
        private string _after;
        private string _before;
        private int _fetchCount;
        private bool _hasBeenPaginated;

        public int? TotalCount => _collection?.Count();

        public PageInfo PageInfo
        {
            get
            {
                if (_pageInfo == null)
                {
                    Paginate(_first, _last, _after, _before);
                }
                return _pageInfo;
            }
        }

        public List<Edge<T>> Edges
        {
            get
            {
                if (_edges != null)
                {
                    return _edges;
                }

                if (_collection == null)
                {
                    _edges = new List<Edge<T>>();
                    return _edges;
                }

                _fetchCount = -1;
                var edges = _collection
                    .Select((item, index) => new Edge<T>
                    {
                        Cursor = $"{index + 1:D8}",
                        Node = item,
                    });

                if (!string.IsNullOrWhiteSpace(_after))
                {
                    int.TryParse(_after, NumberStyles.Any, CultureInfo.InvariantCulture, out _fetchCount);
                    edges = edges.SkipWhile(edge => string.CompareOrdinal(edge.Cursor, _after) <= 0);
                }

                if (!string.IsNullOrWhiteSpace(_before))
                {
                    var beforeEdges = edges
                        .TakeWhile(edge => string.CompareOrdinal(edge.Cursor, _before) <= 0)
                        .ToList();

                    _fetchCount = beforeEdges.Count;

                    edges = beforeEdges
                        .TakeWhile(edge => string.CompareOrdinal(edge.Cursor, _before) < 0);
                }

                if (_first.HasValue)
                {
                    var edgesList = edges.Take(_first.Value).ToList();
                    edges = edgesList;
                    _fetchCount = _fetchCount != -1
                        ? edgesList.Count + 1 + _fetchCount
                        : edgesList.Count + 1;
                }

                if (_last.HasValue)
                {
                    var edgesList = edges.ToList();
                    var count = edgesList.Count;
                    edges = _last.Value < count
                        ? edgesList.Skip(count - _last.Value)
                        : edgesList;
                }

                _edges = edges.ToList();

                if (_fetchCount == -1)
                {
                    _fetchCount = _collection.Count();
                }

                return _edges;
            }
        }

        public List<T> Items => Edges?.Select(edge => edge != null ? edge.Node : default(T)).ToList();

        public Connection<T> FromCollection(IEnumerable<T> collection)
        {
            _collection = collection;
            return this;
        }

        public Connection<T> Paginate(
            int? first = null, int? last = null, string after = null, string before = null)
        {
            if (_hasBeenPaginated)
            {
                throw new Exception("Cannot paginate connection twice.");
            }

            _first = first;
            _last = last;
            _after = after;
            _before = before;
            ValidateParameters();

            _edges = null;
            var edges = Edges;
            var minCursor = $"{0:D8}";
            var maxCursor = $"{0:D8}";

            if (_collection.Any())
            {
                minCursor = $"{1:D8}";
                maxCursor = $"{_fetchCount:D8}";
            }

            if (edges.Any())
            {
                var minPageCursor = edges.Min(edge => edge.Cursor);
                var maxPageCursor = edges.Max(edge => edge.Cursor);

                _pageInfo = new PageInfo
                {
                    StartCursor = minPageCursor,
                    EndCursor = maxPageCursor,
                    HasNextPage = string.CompareOrdinal(maxPageCursor, maxCursor) < 0,
                    HasPreviousPage = string.CompareOrdinal(minPageCursor, minCursor) > 0,
                };
            }
            else
            {
                _pageInfo = new PageInfo
                {
                    StartCursor = after ?? before ?? minCursor,
                    EndCursor = before ?? after ?? maxCursor,
                };

                int startValue, endValue;
                int.TryParse(PageInfo.StartCursor, NumberStyles.Any, CultureInfo.InvariantCulture, out startValue);
                int.TryParse(PageInfo.EndCursor, NumberStyles.Any, CultureInfo.InvariantCulture, out endValue);

                if (!string.IsNullOrWhiteSpace(before))
                {
                    var cursorValue = Math.Min(startValue - 1, endValue - 1);
                    PageInfo.StartCursor = PageInfo.EndCursor = $"{cursorValue:D8}";
                }
                else if (!string.IsNullOrWhiteSpace(after))
                {
                    var cursorValue = Math.Max(startValue + 1, endValue + 1);
                    PageInfo.StartCursor = PageInfo.EndCursor = $"{cursorValue:D8}";
                }
                else if (first.HasValue)
                {
                    var cursorValue = Math.Min(startValue - 1, endValue - 1);
                    PageInfo.StartCursor = PageInfo.EndCursor = $"{cursorValue:D8}";
                }
                else if (last.HasValue)
                {
                    var cursorValue = Math.Max(startValue + 1, endValue + 1);
                    PageInfo.StartCursor = PageInfo.EndCursor = $"{cursorValue:D8}";
                }

                PageInfo.HasNextPage =
                    (_first.GetValueOrDefault(-1) == 0)
                        ? string.CompareOrdinal(PageInfo.EndCursor, maxCursor) <= 0
                        : string.CompareOrdinal(PageInfo.EndCursor, maxCursor) < 0;

                PageInfo.HasPreviousPage =
                    string.CompareOrdinal(PageInfo.StartCursor, minCursor) > 0;
            }

            _hasBeenPaginated = true;
            return this;
        }

        private void ValidateParameters()
        {
            if (_first.HasValue && _last.HasValue)
            {
                throw new ArgumentException("Cannot use `first` in conjunction with `last`.");
            }

            if (!string.IsNullOrWhiteSpace(_after) && !string.IsNullOrWhiteSpace(_before))
            {
                throw new ArgumentException("Cannot use `after` in conjunction with `before`.");
            }

            if (_first.HasValue && !string.IsNullOrWhiteSpace(_before))
            {
                throw new ArgumentException("Cannot use `first` in conjunction with `before`.");
            }

            if (_last.HasValue && !string.IsNullOrWhiteSpace(_after))
            {
                throw new ArgumentException("Cannot use `last` in conjunction with `after`.");
            }
        }
    }
}
