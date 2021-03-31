using System;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Types.Relay;

namespace GraphQL.Builders
{
    /// <summary>
    /// Builds a connection field for graphs that have the specified source type.
    /// </summary>
    public class ConnectionBuilder<TSourceType>
    {
        private bool _isUnidirectional;

        private bool _isBidirectional;

        private int? _pageSize;

        /// <summary>
        /// Returns the generated field.
        /// </summary>
        public FieldType FieldType { get; protected set; }

        private ConnectionBuilder(
            FieldType fieldType,
            bool isUnidirectional,
            bool isBidirectional,
            int? pageSize)
        {
            _isUnidirectional = isUnidirectional;
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
            return new ConnectionBuilder<TSourceType>(fieldType, false, false, null)
                .Unidirectional();
        }

        /// <summary>
        /// Configure the connection to be forward-only.
        /// </summary>
        public ConnectionBuilder<TSourceType> Unidirectional()
        {
            if (_isUnidirectional)
                return this;

            Argument<StringGraphType, string>("after",
                "Only look at connected edges with cursors greater than the value of `after`.");
            Argument<IntGraphType, int?>("first",
                "Specifies the number of edges to return starting from `after` or the first entry if `after` is not specified.");

            _isUnidirectional = true;
            _isBidirectional = false;

            return this;
        }

        /// <summary>
        /// Configure the connection to be bi-directional.
        /// </summary>
        public ConnectionBuilder<TSourceType> Bidirectional()
        {
            if (_isBidirectional)
                return this;

            Argument<StringGraphType, string>("before",
                "Only look at connected edges with cursors smaller than the value of `before`.");
            Argument<IntGraphType, int?>("last",
                "Specifies the number of edges to return counting reversely from `before`, or the last entry if `before` is not specified.");

            _isUnidirectional = false;
            _isBidirectional = true;

            return this;
        }

        /// <inheritdoc cref="FieldBuilder{TSourceType, TReturnType}.Name(string)"/>
        public ConnectionBuilder<TSourceType> Name(string name)
        {
            FieldType.Name = name;
            return this;
        }

        /// <inheritdoc cref="FieldBuilder{TSourceType, TReturnType}.Description(string)"/>
        public ConnectionBuilder<TSourceType> Description(string description)
        {
            FieldType.Description = description;
            return this;
        }

        /// <inheritdoc cref="FieldBuilder{TSourceType, TReturnType}.DeprecationReason(string)"/>
        public ConnectionBuilder<TSourceType> DeprecationReason(string deprecationReason)
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
        /// <param name="description">The description of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public ConnectionBuilder<TSourceType> Argument<TArgumentGraphType>(string name, string description, Action<QueryArgument> configure = null)
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
        public ConnectionBuilder<TSourceType> Argument<TArgumentGraphType, TArgumentType>(string name, string description,
            TArgumentType defaultValue = default, Action<QueryArgument> configure = null)
            where TArgumentGraphType : IGraphType
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                arg.DefaultValue = defaultValue;
                configure?.Invoke(arg);
            });

        /// <summary>
        /// Adds an argument to the connection field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public ConnectionBuilder<TSourceType> Argument<TArgumentGraphType>(string name, Action<QueryArgument> configure = null)
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
        /// Runs a configuration delegate for the connection field.
        /// </summary>
        public virtual ConnectionBuilder<TSourceType> Configure(Action<FieldType> configure)
        {
            configure(FieldType);
            return this;
        }

        /// <summary>
        /// Apply directive to connection field without specifying arguments. If the directive
        /// declaration has arguments, then their default values (if any) will be used.
        /// </summary>
        /// <param name="name">Directive name.</param>
        public virtual ConnectionBuilder<TSourceType> Directive(string name)
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
        public virtual ConnectionBuilder<TSourceType> Directive(string name, string argumentName, object argumentValue)
        {
            FieldType.ApplyDirective(name, argumentName, argumentValue);
            return this;
        }

        /// <summary>
        /// Apply directive to connection field specifying configuration delegate.
        /// </summary>
        /// <param name="name">Directive name.</param>
        /// <param name="configure">Configuration delegate.</param>
        public virtual ConnectionBuilder<TSourceType> Directive(string name, Action<AppliedDirective> configure)
        {
            FieldType.ApplyDirective(name, configure);
            return this;
        }

        /// <summary>
        /// Sets the resolver method for the connection field.
        /// </summary>
        public void Resolve(Func<IResolveConnectionContext<TSourceType>, object> resolver)
        {
            FieldType.Resolver = new FuncFieldResolver<object>(context =>
            {
                var args = new ResolveConnectionContext<TSourceType>(context, _isUnidirectional, _pageSize);
                CheckForErrors(args);
                return resolver(args);
            });
        }

        /// <summary>
        /// Sets the resolver method for the connection field.
        /// </summary>
        public void ResolveAsync(Func<IResolveConnectionContext<TSourceType>, Task<object>> resolver)
        {
            FieldType.Resolver = new AsyncFieldResolver<object>(context =>
            {
                var args = new ResolveConnectionContext<TSourceType>(context, _isUnidirectional, _pageSize);
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
