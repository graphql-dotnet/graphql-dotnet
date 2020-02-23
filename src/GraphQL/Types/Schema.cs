using GraphQL.Conversion;
using GraphQL.Introspection;
using GraphQL.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    /// <inheritdoc cref="ISchema"/>
    public class Schema : MetadataProvider, ISchema, IServiceProvider, IDisposable
    {
        private IServiceProvider _services;
        private Lazy<GraphTypesLookup> _lookup;
        private readonly List<Type> _additionalTypes;
        private readonly List<IGraphType> _additionalInstances;
        private readonly List<DirectiveGraphType> _directives;
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

            _lookup = new Lazy<GraphTypesLookup>(CreateTypesLookup);
            _additionalTypes = new List<Type>();
            _additionalInstances = new List<IGraphType>();
            _directives = new List<DirectiveGraphType>
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

        public INameConverter NameConverter { get; set; } = CamelCaseNameConverter.Instance;

        public bool Initialized => _lookup?.IsValueCreated == true;

        public void Initialize()
        {
            CheckDisposed();

            FindType("____");
        }

        public IObjectGraphType Query { get; set; }

        public IObjectGraphType Mutation { get; set; }

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

        public ISchemaFilter Filter { get; set; } = new DefaultSchemaFilter();

        public IEnumerable<DirectiveGraphType> Directives
        {
            get => _directives;
            set
            {
                CheckDisposed();

                _directives.Clear();

                if (value != null)
                    _directives.AddRange(value);
            }
        }

        public IEnumerable<IGraphType> AllTypes =>
            _lookup?
                .Value
                .All()
                .ToList() ?? (IEnumerable<IGraphType>)Array.Empty<IGraphType>();

        public IEnumerable<Type> AdditionalTypes => _additionalTypes;

        public void RegisterType(IGraphType type)
        {
            CheckDisposed();

            _additionalInstances.Add(type ?? throw new ArgumentNullException(nameof(type)));
        }

        public void RegisterTypes(params IGraphType[] types)
        {
            CheckDisposed();

            foreach (var type in types)
                RegisterType(type);
        }

        public void RegisterTypes(params Type[] types)
        {
            CheckDisposed();

            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            foreach (var type in types)
            {
                RegisterType(type);
            }
        }

        public void RegisterType<T>() where T : IGraphType
        {
            CheckDisposed();

            RegisterType(typeof(T));
        }

        public void RegisterDirective(DirectiveGraphType directive)
        {
            CheckDisposed();

            _directives.Add(directive ?? throw new ArgumentNullException(nameof(directive)));
        }

        public void RegisterDirectives(IEnumerable<DirectiveGraphType> directives)
        {
            CheckDisposed();

            foreach (var directive in directives)
                RegisterDirective(directive);
        }

        public void RegisterDirectives(params DirectiveGraphType[] directives)
        {
            CheckDisposed();

            foreach (var directive in directives)
                RegisterDirective(directive);
        }

        public DirectiveGraphType FindDirective(string name)
        {
            return _directives.FirstOrDefault(x => x.Name == name);
        }

        public void RegisterValueConverter(IAstFromValueConverter converter)
        {
            CheckDisposed();

            _converters.Add(converter ?? throw new ArgumentNullException(nameof(converter)));
        }

        public IAstFromValueConverter FindValueConverter(object value, IGraphType type)
        {
            return _converters.FirstOrDefault(x => x.Matches(value, type));
        }

        public IGraphType FindType(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentOutOfRangeException(nameof(name), "A type name is required to lookup.");
            }

            return _lookup?.Value[name];
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_lookup != null)
                {
                    _services = null;
                    Query = null;
                    Mutation = null;
                    Subscription = null;
                    Filter = null;

                    _additionalInstances.Clear();
                    _additionalTypes.Clear();
                    _directives.Clear();
                    _converters.Clear();

                    if (_lookup.IsValueCreated)
                    {
                        _lookup.Value.Clear(true);
                    }

                    _lookup = null;
                }
            }
        }

        private void CheckDisposed()
        {
            if (_lookup == null)
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

        private GraphTypesLookup CreateTypesLookup()
        {
            var types = _additionalInstances
                .Union(GetRootTypes())
                .Union(_additionalTypes.Select(type => (IGraphType)_services.GetRequiredService(type.GetNamedType())));

            return GraphTypesLookup.Create(
                types,
                _directives,
                type => (IGraphType)_services.GetRequiredService(type),
                NameConverter,
                seal: true);
        }
    }
}
