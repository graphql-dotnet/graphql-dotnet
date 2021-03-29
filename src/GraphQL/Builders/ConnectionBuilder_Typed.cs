using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using GraphQL.Types;
using GraphQL.Types.Relay;

#nullable enable

namespace GraphQL.Builders
{
    /// <summary>
    /// Builds a connection field for graphs that have the specified source type.
    /// </summary>
    public class ConnectionBuilder<TSourceType, TReturnType>
    {
        private bool _isBidirectional;

        private int? _pageSize;

        /// <summary>
        /// Returns the generated field.
        /// </summary>
        public FieldType FieldType { get; protected set; }

        internal ConnectionBuilder(
            FieldType fieldType,
            bool isBidirectional,
            int? pageSize)
        {
            _isBidirectional = isBidirectional;
            _pageSize = pageSize;
            FieldType = fieldType;
        }

        /// <summary>
        /// Returns a builder for new connection field for the specified node type.
        /// The edge type is <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;.
        /// The connection type is <see cref="ConnectionType{TNodeType, TEdgeType}">ConnectionType</see>&lt;<typeparamref name="TNodeType"/>, <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;&gt;.
        /// </summary>
        /// <typeparam name="TNodeType">The graph type of the connection's node.</typeparam>
        public static ConnectionBuilder<TSourceType, TReturnType> Create<TNodeType>(string name = "default")
            where TNodeType : IGraphType => Create<TNodeType, EdgeType<TNodeType>>(name);

        /// <summary>
        /// Returns a builder for new connection field for the specified node and edge type.
        /// The connection type is <see cref="ConnectionType{TNodeType, TEdgeType}">ConnectionType</see>&lt;<typeparamref name="TNodeType"/>, <typeparamref name="TEdgeType"/>&gt;
        /// </summary>
        /// <typeparam name="TNodeType">The graph type of the connection's node.</typeparam>
        /// <typeparam name="TEdgeType">The graph type of the connection's edge. Must derive from <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;.</typeparam>
        public static ConnectionBuilder<TSourceType, TReturnType> Create<TNodeType, TEdgeType>(string name = "default")
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
            => Create<TNodeType, TEdgeType, ConnectionType<TNodeType, TEdgeType>>(name);

        /// <summary>
        /// Returns a builder for new connection field for the specified node, edge and connection type.
        /// </summary>
        /// <typeparam name="TNodeType">The graph type of the connection's node.</typeparam>
        /// <typeparam name="TEdgeType">The graph type of the connection's edge. Must derive from <see cref="EdgeType{TNodeType}">EdgeType</see>&lt;<typeparamref name="TNodeType"/>&gt;.</typeparam>
        /// <typeparam name="TConnectionType">The graph type of the connection. Must derive from <see cref="ConnectionType{TNodeType, TEdgeType}">ConnectionType</see>&lt;<typeparamref name="TNodeType"/>, <typeparamref name="TEdgeType"/>&gt;.</typeparam>
        public static ConnectionBuilder<TSourceType, TReturnType> Create<TNodeType, TEdgeType, TConnectionType>(string name = "default")
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
                Description = "Specifies the number of edges to return starting from `after` or the first entry if `after` is not specified.",
            });

            return new ConnectionBuilder<TSourceType, TReturnType>(fieldType, false, null);
        }

        /// <summary>
        /// Configure the connection to be bi-directional.
        /// </summary>
        public ConnectionBuilder<TSourceType, TReturnType> Bidirectional()
        {
            if (_isBidirectional)
                return this;

            Argument<StringGraphType, string>("before",
                "Only look at connected edges with cursors smaller than the value of `before`.");
            Argument<IntGraphType, int?>("last",
                "Specifies the number of edges to return counting reversely from `before`, or the last entry if `before` is not specified.");

            _isBidirectional = true;

            return this;
        }

        /// <inheritdoc cref="FieldBuilder{TSourceType, TReturnType}.Name(string)"/>
        public ConnectionBuilder<TSourceType, TReturnType> Name(string name)
        {
            FieldType.Name = name;
            return this;
        }

        /// <inheritdoc cref="FieldBuilder{TSourceType, TReturnType}.Description(string)"/>
        public ConnectionBuilder<TSourceType, TReturnType> Description(string? description)
        {
            FieldType.Description = description;
            return this;
        }

        /// <inheritdoc cref="FieldBuilder{TSourceType, TReturnType}.DeprecationReason(string)"/>
        public ConnectionBuilder<TSourceType, TReturnType> DeprecationReason(string? deprecationReason)
        {
            FieldType.DeprecationReason = deprecationReason;
            return this;
        }

        /// <summary>
        /// Sets the default page size.
        /// </summary>
        public ConnectionBuilder<TSourceType, TReturnType> PageSize(int pageSize)
        {
            _pageSize = pageSize;
            return this;
        }

        /// <summary>
        /// Clears the default page size, so all records are returned by default.
        /// </summary>
        public ConnectionBuilder<TSourceType, TReturnType> ReturnAll()
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
        public ConnectionBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType>(string name, Action<QueryArgument>? configure = null)
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
        /// <param name="configure">A delegate to further configure the argument.</param>
        public ConnectionBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType>(string name, string? description, Action<QueryArgument>? configure = null)
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
        /// <param name="configure">A delegate to further configure the argument.</param>
        public ConnectionBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType, TArgumentType>(string name, string? description,
#if NETSTANDARD2_1
            [AllowNull]
#endif
            TArgumentType defaultValue = default!, Action<QueryArgument>? configure = null)
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
        public virtual ConnectionBuilder<TSourceType, TReturnType> Configure(Action<FieldType> configure)
        {
            configure(FieldType);
            return this;
        }

        /// <summary>
        /// Apply directive to connection field without specifying arguments. If the directive
        /// declaration has arguments, then their default values (if any) will be used.
        /// </summary>
        /// <param name="name">Directive name.</param>
        public virtual ConnectionBuilder<TSourceType, TReturnType> Directive(string name)
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
        public virtual ConnectionBuilder<TSourceType, TReturnType> Directive(string name, string argumentName, object? argumentValue)
        {
            FieldType.ApplyDirective(name, argumentName, argumentValue);
            return this;
        }

        /// <summary>
        /// Apply directive to connection field specifying configuration delegate.
        /// </summary>
        /// <param name="name">Directive name.</param>
        /// <param name="configure">Configuration delegate.</param>
        public virtual ConnectionBuilder<TSourceType, TReturnType> Directive(string name, Action<AppliedDirective> configure)
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
            return new ConnectionBuilder<TSourceType, TNewReturnType>(FieldType, _isBidirectional, _pageSize);
        }

        /// <summary>
        /// Sets the resolver method for the connection field.
        /// </summary>
        public ConnectionBuilder<TSourceType, TReturnType> Resolve(Func<IResolveConnectionContext<TSourceType>, TReturnType> resolver)
        {
            FieldType.Resolver = new Resolvers.FuncFieldResolver<TReturnType>(context =>
            {
                var args = new ResolveConnectionContext<TSourceType>(context, !_isBidirectional, _pageSize);
                CheckForErrors(args);
                return resolver(args);
            });
            return this;
        }

        /// <summary>
        /// Sets the resolver method for the connection field.
        /// </summary>
        public ConnectionBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveConnectionContext<TSourceType>, Task<TReturnType>> resolver)
        {
            FieldType.Resolver = new Resolvers.AsyncFieldResolver<TReturnType>(context =>
            {
                var args = new ResolveConnectionContext<TSourceType>(context, !_isBidirectional, _pageSize);
                CheckForErrors(args);
                return resolver(args);
            });
            return this;
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
