using System;
#if NETSTANDARD2_1
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using GraphQL.Types.Relay;

#nullable enable

namespace GraphQL.Builders
{
    /// <summary>
    /// Static methods to create connection field builders.
    /// </summary>
    public static class ConnectionBuilder
    {
        /// <summary>
        /// Returns a builder for new connection field for the specified node type.
        /// The edge type is <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;.
        /// The connection type is <see cref="ConnectionType{TNodeType, TEdgeType}">ConnectionType</see>&lt;<typeparamref name="TNodeType"/>, <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;&gt;.
        /// </summary>
        /// <typeparam name="TNodeType">The graph type of the connection's node.</typeparam>
        /// <typeparam name="TSourceType">The type of <see cref="IResolveFieldContext.Source"/>.</typeparam>
        public static ConnectionBuilder<TSourceType> Create<TNodeType, TSourceType>()
            where TNodeType : IGraphType
            => ConnectionBuilder<TSourceType>.Create<TNodeType>();

        /// <summary>
        /// Returns a builder for new connection field for the specified node and edge type.
        /// The connection type is <see cref="ConnectionType{TNodeType, TEdgeType}">ConnectionType</see>&lt;<typeparamref name="TNodeType"/>, <typeparamref name="TEdgeType"/>&gt;
        /// </summary>
        /// <typeparam name="TNodeType">The graph type of the connection's node.</typeparam>
        /// <typeparam name="TEdgeType">The graph type of the connection's edge. Must derive from <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;.</typeparam>
        /// <typeparam name="TSourceType">The type of <see cref="IResolveFieldContext.Source"/>.</typeparam>
        public static ConnectionBuilder<TSourceType> Create<TNodeType, TEdgeType, TSourceType>()
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
            => ConnectionBuilder<TSourceType>.Create<TNodeType, TEdgeType>();

        /// <summary>
        /// Returns a builder for new connection field for the specified node, edge and connection type.
        /// </summary>
        /// <typeparam name="TNodeType">The graph type of the connection's node.</typeparam>
        /// <typeparam name="TEdgeType">The graph type of the connection's edge. Must derive from <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;.</typeparam>
        /// <typeparam name="TConnectionType">The graph type of the connection. Must derive from <see cref="ConnectionType{TNodeType, TEdgeType}">ConnectionType</see>&lt;<typeparamref name="TNodeType"/>, <typeparamref name="TEdgeType"/>&gt;.</typeparam>
        /// <typeparam name="TSourceType">The type of <see cref="IResolveFieldContext.Source"/>.</typeparam>
        public static ConnectionBuilder<TSourceType> Create<TNodeType, TEdgeType, TConnectionType, TSourceType>()
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
            where TConnectionType : ConnectionType<TNodeType, TEdgeType>
            => ConnectionBuilder<TSourceType>.Create<TNodeType, TEdgeType, TConnectionType>();
    }

    /// <summary>
    /// Builds a connection field for graphs that have the specified source type.
    /// </summary>
    public class ConnectionBuilder<TSourceType>
    {
        internal const string PAGE_SIZE_METADATA_KEY = "__ConnectionBuilder_PageSize";

        private bool _isBidirectional => FieldType.Arguments.Any(x => x.Name == "before");

        private int? _pageSize
        {
            get => FieldType.GetMetadata<int?>(PAGE_SIZE_METADATA_KEY);
            set => FieldType.WithMetadata(PAGE_SIZE_METADATA_KEY, value);
        }

        /// <summary>
        /// Returns the generated field.
        /// </summary>
        public FieldType FieldType { get; protected set; }

        private ConnectionBuilder(FieldType fieldType)
        {
            FieldType = fieldType;
        }

        /// <summary>
        /// Returns a builder for new connection field for the specified node type.
        /// The edge type is <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;.
        /// The connection type is <see cref="ConnectionType{TNodeType, TEdgeType}">ConnectionType</see>&lt;<typeparamref name="TNodeType"/>, <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;&gt;.
        /// </summary>
        /// <typeparam name="TNodeType">The graph type of the connection's node.</typeparam>
        public static ConnectionBuilder<TSourceType> Create<TNodeType>(string name = "default")
            where TNodeType : IGraphType => Create<TNodeType, EdgeType<TNodeType>>(name);

        /// <summary>
        /// Returns a builder for new connection field for the specified node and edge type.
        /// The connection type is <see cref="ConnectionType{TNodeType, TEdgeType}">ConnectionType</see>&lt;<typeparamref name="TNodeType"/>, <typeparamref name="TEdgeType"/>&gt;
        /// </summary>
        /// <typeparam name="TNodeType">The graph type of the connection's node.</typeparam>
        /// <typeparam name="TEdgeType">The graph type of the connection's edge. Must derive from <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;.</typeparam>
        public static ConnectionBuilder<TSourceType> Create<TNodeType, TEdgeType>(string name = "default")
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
            => Create<TNodeType, TEdgeType, ConnectionType<TNodeType, TEdgeType>>(name);

        /// <summary>
        /// Returns a builder for new connection field for the specified node, edge and connection type.
        /// </summary>
        /// <typeparam name="TNodeType">The graph type of the connection's node.</typeparam>
        /// <typeparam name="TEdgeType">The graph type of the connection's edge. Must derive from <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;.</typeparam>
        /// <typeparam name="TConnectionType">The graph type of the connection. Must derive from <see cref="ConnectionType{TNodeType, TEdgeType}">ConnectionType</see>&lt;<typeparamref name="TNodeType"/>, <typeparamref name="TEdgeType"/>&gt;.</typeparam>
        public static ConnectionBuilder<TSourceType> Create<TNodeType, TEdgeType, TConnectionType>(string name = "default")
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
            where TConnectionType : ConnectionType<TNodeType, TEdgeType>
        {
            var fieldType = new FieldType
            {
                Name = name,
                Type = typeof(TConnectionType),
                Arguments = new QueryArguments(),
            };
            fieldType.Arguments.Add(new QueryArgument(typeof(StringGraphType))
            {
                Name = "after",
                Description = "Only look at connected edges with cursors greater than the value of `after`.",
            });
            fieldType.Arguments.Add(new QueryArgument(typeof(IntGraphType))
            {
                Name = "first",
                Description = "Specifies the maximum number of edges to return, starting after the cursor specified by `after`, or the first number of edges if `after` is not specified.",
            });
            return new ConnectionBuilder<TSourceType>(fieldType);
        }

        /// <summary>
        /// Configure the connection to be forward-only.
        /// </summary>
        [Obsolete("Calling Unidirectional is unnecessary and will be removed in future versions.")]
        public ConnectionBuilder<TSourceType> Unidirectional()
        {
            if (_isBidirectional)
                throw new InvalidOperationException("Cannot call Unidirectional after a call to Bidirectional.");

            return this;
        }

        /// <summary>
        /// Configure the connection to be bi-directional.
        /// </summary>
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

            return this;
        }

        /// <inheritdoc cref="FieldBuilder{TSourceType, TReturnType}.Name(string)"/>
        public ConnectionBuilder<TSourceType> Name(string name)
        {
            FieldType.Name = name;
            return this;
        }

        /// <inheritdoc cref="FieldBuilder{TSourceType, TReturnType}.Description(string)"/>
        public ConnectionBuilder<TSourceType> Description(string? description)
        {
            FieldType.Description = description;
            return this;
        }

        /// <inheritdoc cref="FieldBuilder{TSourceType, TReturnType}.DeprecationReason(string)"/>
        public ConnectionBuilder<TSourceType> DeprecationReason(string? deprecationReason)
        {
            FieldType.DeprecationReason = deprecationReason;
            return this;
        }

        /// <summary>
        /// Sets the default page size.
        /// </summary>
        public ConnectionBuilder<TSourceType> PageSize(int pageSize)
        {
            _pageSize = pageSize;
            return this;
        }

        /// <summary>
        /// Clears the default page size, so all records are returned by default.
        /// </summary>
        public ConnectionBuilder<TSourceType> ReturnAll()
        {
            _pageSize = null;
            return this;
        }

        /// <summary>
        /// Adds an argument to the connection field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public ConnectionBuilder<TSourceType> Argument<TArgumentGraphType>(string name, Action<QueryArgument>? configure = null)
            where TArgumentGraphType : IGraphType
        {
            var arg = new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
            };
            configure?.Invoke(arg);
            FieldType.Arguments.Add(arg);
            return this;
        }

        /// <summary>
        /// Adds an argument to the connection field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="description">The description of the argument.</param>
        public ConnectionBuilder<TSourceType> Argument<TArgumentGraphType>(string name, string? description)
            where TArgumentGraphType : IGraphType
            => Argument<TArgumentGraphType>(name, arg => arg.Description = description);

        /// <summary>
        /// Adds an argument to the connection field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="description">The description of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public ConnectionBuilder<TSourceType> Argument<TArgumentGraphType>(string name, string? description, Action<QueryArgument>? configure)
            where TArgumentGraphType : IGraphType
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                configure?.Invoke(arg);
            });

        /// <summary>
        /// Adds an argument to the connection field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <typeparam name="TArgumentType">The type of the argument value.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="description">The description of the argument.</param>
        /// <param name="defaultValue">The default value of the argument.</param>
        public ConnectionBuilder<TSourceType> Argument<TArgumentGraphType, TArgumentType>(string name, string? description,
#if NETSTANDARD2_1
            [AllowNull]
#endif
            TArgumentType defaultValue = default!)
            where TArgumentGraphType : IGraphType
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                arg.DefaultValue = defaultValue;
            });

        /// <summary>
        /// Adds an argument to the connection field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <typeparam name="TArgumentType">The type of the argument value.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="description">The description of the argument.</param>
        /// <param name="defaultValue">The default value of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public ConnectionBuilder<TSourceType> Argument<TArgumentGraphType, TArgumentType>(string name, string? description,
#if NETSTANDARD2_1
            [AllowNull]
#endif
            TArgumentType defaultValue, Action<QueryArgument>? configure)
            where TArgumentGraphType : IGraphType
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                arg.DefaultValue = defaultValue;
                configure?.Invoke(arg);
            });

        /// <summary>
        /// Runs a configuration delegate for the connection field.
        /// </summary>
        public ConnectionBuilder<TSourceType> Configure(Action<FieldType> configure)
        {
            configure(FieldType);
            return this;
        }

        /// <summary>
        /// Apply directive to connection field without specifying arguments. If the directive
        /// declaration has arguments, then their default values (if any) will be used.
        /// </summary>
        /// <param name="name">Directive name.</param>
        public ConnectionBuilder<TSourceType> Directive(string name)
        {
            FieldType.ApplyDirective(name);
            return this;
        }

        /// <summary>
        /// Apply directive to connection field specifying one argument. If the directive
        /// declaration has other arguments, then their default values (if any) will be used.
        /// </summary>
        /// <param name="name">Directive name.</param>
        /// <param name="argumentName">Argument name.</param>
        /// <param name="argumentValue">Argument value.</param>
        public ConnectionBuilder<TSourceType> Directive(string name, string argumentName, object? argumentValue)
        {
            FieldType.ApplyDirective(name, argumentName, argumentValue);
            return this;
        }

        /// <summary>
        /// Apply directive to connection field specifying configuration delegate.
        /// </summary>
        /// <param name="name">Directive name.</param>
        /// <param name="configure">Configuration delegate.</param>
        public ConnectionBuilder<TSourceType> Directive(string name, Action<AppliedDirective> configure)
        {
            FieldType.ApplyDirective(name, configure);
            return this;
        }

        /// <summary>
        /// Sets the return type of the field.
        /// </summary>
        /// <typeparam name="TNewReturnType">The type of the return value of the resolver.</typeparam>
        public ConnectionBuilder<TSourceType, TNewReturnType> Returns<TNewReturnType>()
        {
            return new ConnectionBuilder<TSourceType, TNewReturnType>(FieldType);
        }

        /// <summary>
        /// Sets the resolver method for the connection field.
        /// </summary>
        public void Resolve(Func<IResolveConnectionContext<TSourceType>, object> resolver)
        {
            FieldType.Resolver = new Resolvers.FuncFieldResolver<object>(context =>
            {
                var args = new ResolveConnectionContext<TSourceType>(context, !_isBidirectional, _pageSize);
                CheckForErrors(args);
                return resolver(args);
            });
        }

        /// <summary>
        /// Sets the resolver method for the connection field.
        /// </summary>
        public void ResolveAsync(Func<IResolveConnectionContext<TSourceType>, Task<object>> resolver)
        {
            FieldType.Resolver = new Resolvers.AsyncFieldResolver<object>(context =>
            {
                var args = new ResolveConnectionContext<TSourceType>(context, !_isBidirectional, _pageSize);
                CheckForErrors(args);
                return resolver(args);
            });
        }

        private void CheckForErrors(IResolveConnectionContext<TSourceType> args)
        {
            if (args.First.HasValue && args.Last.HasValue)
            {
                throw new ArgumentException("Cannot specify both `first` and `last`.");
            }
            if (args.IsUnidirectional && args.Last.HasValue)
            {
                throw new ArgumentException("Cannot use `last` with unidirectional connections.");
            }
        }
    }
}
