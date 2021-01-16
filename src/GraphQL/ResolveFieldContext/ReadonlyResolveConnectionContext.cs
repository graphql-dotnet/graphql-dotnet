using System;
using GraphQL.Execution;

namespace GraphQL
{
    /// <summary>
    /// A mutable implementation of <see cref="IResolveConnectionContext{T}"/>
    /// </summary>
    public class ReadonlyResolveConnectionContext<T> : ReadonlyResolveFieldContext<T>, IResolveConnectionContext<T>
    {
        private readonly int? _defaultPageSize;

        /// <summary>
        /// Initializes an instance with the specified <see cref="ExecutionNode"/>, <see cref="ExecutionContext"/> and other properties.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="context"></param>
        /// <param name="isUnidirectional">Indicates if the connection only allows forward paging requests</param>
        /// <param name="defaultPageSize">Indicates the default page size if not specified by the request</param>
        public ReadonlyResolveConnectionContext(ExecutionNode node, ExecutionContext context, bool isUnidirectional, int? defaultPageSize)
            : base(node, context)
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
        public string After => this.GetArgument<string>("after");

        /// <inheritdoc/>
        public string Before => this.GetArgument<string>("before");

        /// <inheritdoc/>
        public int? PageSize => First ?? Last ?? _defaultPageSize;
    }
}
