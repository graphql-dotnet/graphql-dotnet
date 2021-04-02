using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Types;
using GraphQL.Types.Relay;

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
    public class ConnectionBuilder<TSourceType> : IProvideMetadata
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
        public ConnectionBuilder<TSourceType> Argument<TArgumentGraphType>(string name, string description)
            where TArgumentGraphType : IGraphType
        {
            FieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
            });
            return this;
        }

        /// <summary>
        /// Adds an argument to the connection field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <typeparam name="TArgumentType">The type of the argument value.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="description">The description of the argument.</param>
        /// <param name="defaultValue">The default value of the argument.</param>
        public ConnectionBuilder<TSourceType> Argument<TArgumentGraphType, TArgumentType>(string name, string description,
            TArgumentType defaultValue = default)
            where TArgumentGraphType : IGraphType
        {
            FieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
                DefaultValue = defaultValue,
            });
            return this;
        }

        /// <summary>
        /// Sets the resolver method for the connection field.
        /// </summary>
        public void Resolve(Func<IResolveConnectionContext<TSourceType>, object> resolver)
        {
            FieldType.Resolver = new Resolvers.FuncFieldResolver<object>(context =>
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
            FieldType.Resolver = new Resolvers.AsyncFieldResolver<object>(context =>
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

        Dictionary<string, object> IProvideMetadata.Metadata => FieldType.Metadata;

        TType IProvideMetadata.GetMetadata<TType>(string key, TType defaultValue) => FieldType.GetMetadata(key, defaultValue);

        TType IProvideMetadata.GetMetadata<TType>(string key, Func<TType> defaultValueFactory) => FieldType.GetMetadata(key, defaultValueFactory);

        bool IProvideMetadata.HasMetadata(string key) => FieldType.HasMetadata(key);
    }
}
