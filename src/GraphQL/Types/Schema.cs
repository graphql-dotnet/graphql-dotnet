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
        private readonly List<Type> _additionalTypes;
        private readonly List<IGraphType> _additionalInstances;
        private readonly List<IAstFromValueConverter> _converters;

        /// <summary>
        /// Create an instance of <see cref="Schema"/> with the <see cref="DefaultServiceProvider"/>, which
        /// uses <see cref="Activator.CreateInstance(Type)"/> to create required objects
        /// </summary>
        public Schema()
            : this(new DefaultServiceProvider())
        {
        }

        /// <summary>
        /// Create an instance of <see cref="Schema"/> with a specified <see cref="IServiceProvider"/>, used
        /// to create required objects
        /// </summary>
        public Schema(IServiceProvider services)
        {
            _services = services;

            _additionalTypes = new List<Type>();
            _additionalInstances = new List<IGraphType>();
            Directives = new SchemaDirectives
            {
                DirectiveGraphType.Include,
                DirectiveGraphType.Skip,
                DirectiveGraphType.Deprecated
            };
            _converters = new List<IAstFromValueConverter>();
        }

        public static ISchema For(string[] typeDefinitions, Action<SchemaBuilder> configure = null)
        {
            var defs = string.Join("\n", typeDefinitions);
            return For(defs, configure);
        }

        public static ISchema For(string typeDefinitions, Action<SchemaBuilder> configure = null)
        {
            var builder = new SchemaBuilder();
            configure?.Invoke(builder);
            return builder.Build(typeDefinitions);
        }

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
        public IEnumerable<Type> AdditionalTypes => _additionalTypes;

        /// <inheritdoc/>
        public FieldType SchemaMetaFieldType => AllTypes.SchemaMetaFieldType;

        /// <inheritdoc/>
        public FieldType TypeMetaFieldType => AllTypes.TypeMetaFieldType;

        /// <inheritdoc/>
        public FieldType TypeNameMetaFieldType => AllTypes.TypeNameMetaFieldType;

        /// <inheritdoc/>
        public void RegisterType(IGraphType type)
        {
            CheckDisposed();
            CheckInitialized();

            _additionalInstances.Add(type ?? throw new ArgumentNullException(nameof(type)));
        }

        /// <inheritdoc/>
        public void RegisterTypes(params IGraphType[] types)
        {
            CheckDisposed();
            CheckInitialized();

            foreach (var type in types)
                RegisterType(type);
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

        /// <inheritdoc/>
        public void RegisterType<T>() where T : IGraphType
        {
            CheckDisposed();
            CheckInitialized();

            RegisterType(typeof(T));
        }

        /// <inheritdoc/>
        public void RegisterDirective(DirectiveGraphType directive)
        {
            CheckDisposed();
            CheckInitialized();

            Directives.Add(directive ?? throw new ArgumentNullException(nameof(directive)));
        }

        public void RegisterDirectives(IEnumerable<DirectiveGraphType> directives)
        {
            CheckDisposed();
            CheckInitialized();

            foreach (var directive in directives)
                RegisterDirective(directive);
        }

        /// <inheritdoc/>
        public void RegisterDirectives(params DirectiveGraphType[] directives)
        {
            CheckDisposed();
            CheckInitialized();

            foreach (var directive in directives)
                RegisterDirective(directive);
        }

        /// <inheritdoc/>
        public DirectiveGraphType FindDirective(string name)
        {
            return Directives.FirstOrDefault(x => x.Name == name);
        }

        /// <inheritdoc/>
        public void RegisterValueConverter(IAstFromValueConverter converter)
        {
            CheckDisposed();

            _converters.Add(converter ?? throw new ArgumentNullException(nameof(converter)));
        }

        /// <inheritdoc/>
        public IAstFromValueConverter FindValueConverter(object value, IGraphType type)
        {
            return _converters.FirstOrDefault(x => x.Matches(value, type));
        }

        /// <inheritdoc/>
        public IGraphType FindType(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentOutOfRangeException(nameof(name), "A type name is required to lookup.");
            }

            return AllTypes[name];
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

                    _additionalInstances.Clear();
                    _additionalTypes.Clear();
                    Directives.List.Clear();
                    _converters.Clear();

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

        private void RegisterType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!typeof(IGraphType).IsAssignableFrom(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Type must be of IGraphType.");
            }

            if (!_additionalTypes.Contains(type))
            {
                _additionalTypes.Add(type);
            }
        }

        private IEnumerable<IGraphType> GetRootTypes()
        {
            //TODO: According to the specification, Query is a required type. But if you uncomment these lines, then the mass of tests begin to fail, because they do not set Query.
            // if (Query == null)
            //    throw new InvalidOperationException("Query root type must be provided. See https://graphql.github.io/graphql-spec/June2018/#sec-Schema-Introspection");

            if (Query != null)
                yield return Query;

            if (Mutation != null)
                yield return Mutation;

            if (Subscription != null)
                yield return Subscription;
        }

        private void CreateSchemaTypes()
        {
            var types = _additionalInstances
                .Union(GetRootTypes())
                .Union(_additionalTypes.Select(type => (IGraphType)_services.GetRequiredService(type.GetNamedType())));

            _allTypes = SchemaTypes.Create(
                types,
                Directives,
                type => (IGraphType)_services.GetRequiredService(type),
                NameConverter);

            // At this point, Initialized will return false, and Initialize will still lock while waiting for initialization to complete.
            // However, AllTypes and similar properties will return a reference to SchemaTypes without waiting for a lock.
            _allTypes.ApplyMiddleware(FieldMiddleware);
        }
    }
}
