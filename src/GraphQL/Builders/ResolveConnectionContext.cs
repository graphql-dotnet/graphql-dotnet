using System;
using GraphQL.Types;

namespace GraphQL.Builders
{
    public class ResolveConnectionContext<T> : ResolveFieldContext<T>
    {
        private readonly int? _defaultPageSize;

        public ResolveConnectionContext(ResolveFieldContext context, bool isUnidirectional, int? defaultPageSize)
                : base(context)
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

}
