using System;

namespace GraphQL.Builders
{
    internal class ResolveConnectionContextAdapter<T> : ResolveFieldContextAdapter<T>, IResolveConnectionContext<T>
    {
        private readonly int? _defaultPageSize;

        public ResolveConnectionContextAdapter(IResolveFieldContext context, bool isUnidirectional, int? defaultPageSize)
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
                var first = FirstInternal;
                if (!first.HasValue && !Last.HasValue)
                {
                    return _defaultPageSize;
                }

                return first;
            }
        }

        private int? FirstInternal
        {
            get
            {
                var first = this.GetArgument<int?>("first");
                return first.HasValue ? (int?)Math.Abs(first.Value) : null;
            }
        }

        public int? Last
        {
            get
            {
                var last = this.GetArgument<int?>("last");
                return last.HasValue ? (int?)Math.Abs(last.Value) : null;
            }
        }

        public string After => this.GetArgument<string>("after");

        public string Before => this.GetArgument<string>("before");

        public int? PageSize => First ?? Last ?? _defaultPageSize;
    }
}
