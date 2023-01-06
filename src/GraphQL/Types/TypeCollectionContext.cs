namespace GraphQL.Types
{
    /// <summary>
    /// Provides a mechanism to resolve graph type instances from their .NET types,
    /// and also to register new graph type instances with their name in the graph type lookup table.
    /// (See <see cref="SchemaTypes"/>.)
    /// </summary>
    internal sealed class TypeCollectionContext
    {
        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="resolver">A delegate which returns an instance of a graph type from its .NET type.</param>
        /// <param name="addType">A delegate which adds a graph type instance to the list of named graph types for the schema.</param>
        /// <param name="typeMappings">CLR-GraphType type mappings.</param>
        /// <param name="schema">The schema.</param>
        internal TypeCollectionContext(Func<Type, IGraphType> resolver, Action<string, IGraphType, TypeCollectionContext> addType, IEnumerable<IGraphTypeMappingProvider>? typeMappings, ISchema schema)
        {
            ResolveType = resolver;
            AddType = addType;
            ClrToGraphTypeMappings = typeMappings;
            Schema = schema;
            if (GlobalSwitches.TrackGraphTypeInitialization)
                InitializationTrace = new();
        }

        /// <summary>
        /// Returns a delegate which returns an instance of a graph type from its .NET type.
        /// </summary>
        internal Func<Type, IGraphType> ResolveType { get; }

        /// <summary>
        /// Returns a delegate which adds a graph type instance to the list of named graph types for the schema.
        /// </summary>
        internal Action<string, IGraphType, TypeCollectionContext> AddType { get; }

        internal IEnumerable<IGraphTypeMappingProvider>? ClrToGraphTypeMappings { get; }

        internal Stack<Type> InFlightRegisteredTypes { get; } = new();

        internal ISchema Schema { get; }

        internal List<string>? InitializationTrace { get; set; }

        internal TypeCollectionContextInitializationTrace Trace(string traceElement) =>
            InitializationTrace == null
                ? default
                : new(this, traceElement);

        internal TypeCollectionContextInitializationTrace Trace(string traceElement, object? arg1)
        {
            return InitializationTrace == null
                ? default
                : new(this, string.Format(traceElement, arg1));
        }

        internal TypeCollectionContextInitializationTrace Trace(string traceElement, object? arg1, object? arg2)
        {
            return InitializationTrace == null
                ? default
                : new(this, string.Format(traceElement, arg1, arg2));
        }
    }

    internal readonly struct TypeCollectionContextInitializationTrace : IDisposable
    {
        private readonly TypeCollectionContext _context;

        public TypeCollectionContextInitializationTrace(TypeCollectionContext context, string traceElement)
        {
            _context = context;
            context.InitializationTrace?.Add(traceElement);
        }

        public void Dispose()
        {
            _context.InitializationTrace?.RemoveAt(_context.InitializationTrace.Count - 1);
        }
    }
}
