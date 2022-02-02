namespace GraphQL.Builders
{
    /// <summary>
    /// A mutable implementation of <see cref="IResolveConnectionContext{T}"/>
    /// </summary>
    public class ResolveConnectionContext<T> : ResolveFieldContext<T>, IResolveConnectionContext<T>
    {
        private readonly int? _defaultPageSize;

        /// <summary>
        /// Initializes an instance which mirrors the specified <see cref="IResolveFieldContext"/>
        /// with the specified properties and defaults
        /// </summary>
        /// <param name="context">The underlying <see cref="IResolveFieldContext"/> to mirror</param>
        /// <param name="isUnidirectional">Indicates if the connection only allows forward paging requests</param>
        /// <param name="defaultPageSize">Indicates the default page size if not specified by the request</param>
        public ResolveConnectionContext(IResolveFieldContext context, bool isUnidirectional, int? defaultPageSize)
            : base(context)
        {
            IsUnidirectional = isUnidirectional;
            _defaultPageSize = defaultPageSize;
        }

        /// <inheritdoc/>
        public bool IsUnidirectional { get; private set; }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public int? Last
        {
            get
            {
                var last = this.GetArgument<int?>("last");
                return last.HasValue ? (int?)Math.Abs(last.Value) : null;
            }
        }

        /// <inheritdoc/>
        public string? After => this.GetArgument<string>("after");

        /// <inheritdoc/>
        public string? Before => this.GetArgument<string>("before");

        /// <inheritdoc/>
        public int? PageSize => First ?? Last ?? _defaultPageSize;
    }
}
