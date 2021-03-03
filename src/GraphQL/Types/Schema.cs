using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Conversion;
using GraphQL.Instrumentation;
using GraphQL.Introspection;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <inheritdoc cref="ISchema"/>
    public class Schema : MetadataProvider, ISchema, IServiceProvider, IDisposable
    {
        private bool _disposed;
        private IServiceProvider _services;
        private SchemaTypes _allTypes;
        private readonly object _allTypesInitializationLock = new object();

        private List<Type> _additionalTypes;
        private List<IGraphType> _additionalInstances;

        private List<Type> _visitorTypes;
        private List<ISchemaNodeVisitor> _visitors;

        /// <summary>
        /// Create an instance of <see cref="Schema"/> with the <see cref="DefaultServiceProvider"/>, which
        /// uses <see cref="Activator.CreateInstance(Type)"/> to create required objects.
        /// </summary>
        public Schema()
            : this(new DefaultServiceProvider())
        {
        }

        /// <summary>
        /// Create an instance of <see cref="Schema"/> with a specified <see cref="IServiceProvider"/>, used
        /// to create required objects.
        /// </summary>
        public Schema(IServiceProvider services)
        {
            _services = services;

            Directives = new SchemaDirectives();
            Directives.Register(DirectiveGraphType.Include, DirectiveGraphType.Skip, DirectiveGraphType.Deprecated);
            RegisterVisitor(DeprecatedDirectiveVisitor.Instance);
        }

        /// <summary>
        /// Builds schema from the specified string and configuration delegate.
        /// </summary>
        /// <param name="typeDefinitions">A textual description of the schema in SDL (Schema Definition Language) format.</param>
        /// <param name="configure">Optional configuration delegate to setup <see cref="SchemaBuilder"/>.</param>
        /// <returns>Created schema.</returns>
        public static Schema For(string typeDefinitions, Action<SchemaBuilder> configure = null)
            => For<SchemaBuilder>(typeDefinitions, configure);

        /// <summary>
        /// Builds schema from the specified string and configuration delegate.
        /// </summary>
        /// <typeparam name="TSchemaBuilder">The type of <see cref="SchemaBuilder"/> that will create the schema.</typeparam>
        /// <param name="typeDefinitions">A textual description of the schema in SDL (Schema Definition Language) format.</param>
        /// <param name="configure">Optional configuration delegate to setup <see cref="SchemaBuilder"/>.</param>
        /// <returns>Created schema.</returns>
        public static Schema For<TSchemaBuilder>(string typeDefinitions, Action<TSchemaBuilder> configure = null)
            where TSchemaBuilder : SchemaBuilder, new()
        {
            var builder = new TSchemaBuilder();
            configure?.Invoke(builder);
            return builder.Build(typeDefinitions);
        }

        /// <inheritdoc/>
        public ExperimentalFeatures Features { get; set; } = new ExperimentalFeatures();

        /// <inheritdoc/>
        public INameConverter NameConverter { get; set; } = CamelCaseNameConverter.Instance;

        /// <inheritdoc/>
        public IFieldMiddlewareBuilder FieldMiddleware { get; internal set; } = new FieldMiddlewareBuilder();

        /// <inheritdoc/>
        public bool Initialized { get; private set; }

        // TODO: It would be worthwhile to think at all about how to redo the design so that such a situation does not arise.
        private void CheckInitialized([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            if (Initialized)
                throw new InvalidOperationException($"Schema is already initialized and sealed for modifications. You should call '{name}' only when Schema.Initialized = false.");
        }

        /// <inheritdoc/>
        public void Initialize()
        {
            CheckDisposed();

            if (Initialized)
                return;

            lock (_allTypesInitializationLock)
            {
                if (Initialized)
                    return;

                CreateSchemaTypes();

                Initialized = true;
            }
        }

        /// <inheritdoc/>
        public string Description { get; set; }

        /// <inheritdoc/>
        public IObjectGraphType Query { get; set; }

        /// <inheritdoc/>
        public IObjectGraphType Mutation { get; set; }

        /// <inheritdoc/>
        public IObjectGraphType Subscription { get; set; }

        /// <summary>
        /// Gets the service object of the specified type. Schema itself acts as a service provider used to
        /// create objects, such as graph types, requested by the schema.
        /// <br/><br/>
        /// Note that most objects are created during schema initialization, which then have the same lifetime
        /// as the schema's lifetime.
        /// <br/><br/>
        /// Other types created by the service provider may include directive visitors, middlewares, validation
        /// rules, and name converters, among others.
        /// <br/><br/>
        /// Explicit implementation of the <see cref="IServiceProvider.GetService"/> method makes this method
        /// less visible to the calling code, which reduces the likelihood of using it as so called ServiceLocator
        /// anti-pattern. However, in some advanced scenarios this may be necessary.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>
        /// A service object of type <paramref name="serviceType"/> or <c>null</c> if there is no service
        /// object of type serviceType.
        /// </returns>
        object IServiceProvider.GetService(Type serviceType) => _services.GetService(serviceType);

        /// <inheritdoc/>
        public ISchemaFilter Filter { get; set; } = new DefaultSchemaFilter();

        /// <inheritdoc/>
        public ISchemaComparer Comparer { get; set; } = new DefaultSchemaComparer();

        /// <inheritdoc/>
        public SchemaDirectives Directives { get; }

        /// <inheritdoc/>
        public SchemaTypes AllTypes
        {
            get
            {
                if (_allTypes == null)
                    Initialize();

                return _allTypes;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<Type> AdditionalTypes => _additionalTypes ?? Enumerable.Empty<Type>();

        /// <inheritdoc/>
        public FieldType SchemaMetaFieldType => AllTypes.SchemaMetaFieldType;

        /// <inheritdoc/>
        public FieldType TypeMetaFieldType => AllTypes.TypeMetaFieldType;

        /// <inheritdoc/>
        public FieldType TypeNameMetaFieldType => AllTypes.TypeNameMetaFieldType;

        /// <inheritdoc/>
        public void RegisterVisitor(ISchemaNodeVisitor visitor)
        {
            CheckDisposed();
            CheckInitialized();

            (_visitors ??= new List<ISchemaNodeVisitor>()).Add(visitor ?? throw new ArgumentNullException(nameof(visitor)));
        }

        /// <inheritdoc/>
        public void RegisterVisitor(Type type)
        {
            CheckDisposed();
            CheckInitialized();

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!typeof(ISchemaNodeVisitor).IsAssignableFrom(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Type must be of ISchemaNodeVisitor.");
            }

            if (!(_visitorTypes ??= new List<Type>()).Contains(type))
                _visitorTypes.Add(type);
        }

        /// <inheritdoc/>
        public void RegisterType(IGraphType type)
        {
            CheckDisposed();
            CheckInitialized();

            (_additionalInstances ??= new List<IGraphType>()).Add(type ?? throw new ArgumentNullException(nameof(type)));
        }

        /// <inheritdoc/>
        public void RegisterType(Type type)
        {
            CheckDisposed();
            CheckInitialized();

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!typeof(IGraphType).IsAssignableFrom(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Type must be of IGraphType.");
            }

            _additionalTypes ??= new List<Type>();

            if (!_additionalTypes.Contains(type))
                _additionalTypes.Add(type);
        }

        /// <inheritdoc/>
        public void RegisterTypes(params Type[] types)
        {
            CheckDisposed();
            CheckInitialized();

            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            foreach (var type in types)
            {
                RegisterType(type);
            }
        }

        private List<(Type clrType, Type graphType)> _clrToGraphTypeMappings;

        /// <inheritdoc/>
        public void RegisterTypeMapping(Type clrType, Type graphType)
        {
            (_clrToGraphTypeMappings ??= new List<(Type, Type)>()).Add((clrType ?? throw new ArgumentNullException(nameof(clrType)), graphType ?? throw new ArgumentNullException(nameof(graphType))));
        }

        /// <inheritdoc/>
        public IEnumerable<(Type clrType, Type graphType)> TypeMappings => _clrToGraphTypeMappings ?? Enumerable.Empty<(Type, Type)>();

        /// <inheritdoc/>
        public IEnumerable<(Type clrType, Type graphType)> BuiltInTypeMappings
        {
            get
            {
                foreach (var pair in SchemaTypes.BuiltInScalarMappings)
                    yield return (pair.Key, pair.Value);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposed)
                {
                    _services = null;
                    Query = null;
                    Mutation = null;
                    Subscription = null;
                    Filter = null;

                    _additionalInstances?.Clear();
                    _additionalTypes?.Clear();
                    Directives.List.Clear();
                    _visitors?.Clear();
                    _visitorTypes?.Clear();

                    _allTypes?.Dictionary.Clear();
                    _allTypes = null;

                    _disposed = true;
                }
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Schema));
        }

        private void CreateSchemaTypes()
        {
            IEnumerable<IGraphType> GetTypes()
            {
                if (_additionalInstances != null)
                {
                    foreach (var instance in _additionalInstances)
                        yield return instance;
                }

                //TODO: According to the specification, Query is a required type. But if you uncomment these lines, then the mass of tests begin to fail, because they do not set Query.
                // if (Query == null)
                //    throw new InvalidOperationException("Query root type must be provided. See https://graphql.github.io/graphql-spec/June2018/#sec-Schema-Introspection");

                if (Query != null)
                    yield return Query;

                if (Mutation != null)
                    yield return Mutation;

                if (Subscription != null)
                    yield return Subscription;

                if (_additionalTypes != null)
                {
                    foreach (var type in _additionalTypes)
                        yield return (IGraphType)_services.GetRequiredService(type.GetNamedType());
                }
            }

            IEnumerable<ISchemaNodeVisitor> GetVisitors()
            {
                if (_visitors != null)
                {
                    foreach (var visitor in _visitors)
                        yield return visitor;
                }

                if (_visitorTypes != null)
                {
                    foreach (var type in _visitorTypes)
                        yield return (ISchemaNodeVisitor)_services.GetRequiredService(type);
                }
            }

            _allTypes = SchemaTypes.Create(
                GetTypes(),
                _clrToGraphTypeMappings,
                Directives,
                type => (IGraphType)_services.GetRequiredService(type),
                this);

            // At this point, Initialized will return false, and Initialize will still lock while waiting for initialization to complete.
            // However, AllTypes and similar properties will return a reference to SchemaTypes without waiting for a lock.
            _allTypes.ApplyMiddleware(FieldMiddleware);

            foreach (var visitor in GetVisitors())
                visitor.Run(this);

            Validate();
        }

        /// <summary>
        /// Validates correctness of the created schema. This method is called only once - during schema initialization.
        /// </summary>
        protected virtual void Validate()
        {
            //TODO: add different validations, also see SchemaBuilder.Validate
            //TODO: checks for parsed SDL may be expanded in the future, see https://github.com/graphql/graphql-spec/issues/653
            SchemaValidationVisitor.Instance.Run(this);
            AppliedDirectivesValidationVisitor.Instance.Run(this);
        }
    }
}
