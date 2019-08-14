using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Conversion;
using GraphQL.Introspection;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    public interface ISchema : IDisposable
    {
        bool Initialized { get; }

        void Initialize();

        IFieldNameConverter FieldNameConverter { get; set;}

        IObjectGraphType Query { get; set; }

        IObjectGraphType Mutation { get; set; }

        IObjectGraphType Subscription { get; set; }

        IEnumerable<DirectiveGraphType> Directives { get; set; }

        IEnumerable<IGraphType> AllTypes { get; }

        IGraphType FindType(string name);

        DirectiveGraphType FindDirective(string name);

        IEnumerable<Type> AdditionalTypes { get; }

        void RegisterType(IGraphType type);

        void RegisterTypes(params IGraphType[] types);

        void RegisterTypes(params Type[] types);

        void RegisterType<T>() where T : IGraphType;

        void RegisterDirective(DirectiveGraphType directive);

        void RegisterDirectives(params DirectiveGraphType[] directives);

        void RegisterValueConverter(IAstFromValueConverter converter);

        IAstFromValueConverter FindValueConverter(object value, IGraphType type);

        /// <summary>
        /// Provides the ability to filter the schema upon introspection to hide types.
        /// </summary>
        ISchemaFilter Filter { get; set; }
    }

    public class Schema : ISchema
    {
        private readonly Lazy<GraphTypesLookup> _lookup;
        private readonly List<Type> _additionalTypes;
        private readonly List<IGraphType> _additionalInstances;
        private readonly List<DirectiveGraphType> _directives;
        private readonly List<IAstFromValueConverter> _converters;

        public Schema()
            : this(new DefaultServiceProvider())
        {
        }

        [Obsolete("Use System.IServiceProvider instead.")]
        public Schema(IDependencyResolver dependencyResolver)
            : this(new DependencyResolverToServiceProviderAdapter(dependencyResolver))
        {
        }

        public Schema(IServiceProvider services)
        {
            Services = services;

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

        public IFieldNameConverter FieldNameConverter { get; set;} = new CamelCaseFieldNameConverter();

        public bool Initialized => _lookup.IsValueCreated;

        public void Initialize()
        {
            FindType("____");
        }

        public IObjectGraphType Query { get; set; }

        public IObjectGraphType Mutation { get; set; }

        public IObjectGraphType Subscription { get; set; }

        public IServiceProvider Services { get; set; }

        /// <summary>
        /// Provides the ability to filter the schema upon introspection to hide types.
        /// </summary>
        public ISchemaFilter Filter { get; set; } = new DefaultSchemaFilter();

        public IEnumerable<DirectiveGraphType> Directives
        {
            get => _directives;
            set
            {
                if (value == null)
                {
                    return;
                }

                _directives.Clear();
                _directives.AddRange(value);
            }
        }

        public IEnumerable<IGraphType> AllTypes =>
            _lookup
                .Value
                .All()
                .ToList();

        public IEnumerable<Type> AdditionalTypes => _additionalTypes;

        public void RegisterType(IGraphType type)
        {
            _additionalInstances.Add(type ?? throw new ArgumentNullException(nameof(type)));
        }

        public void RegisterTypes(params IGraphType[] types)
        {
            foreach (var type in types)
                RegisterType(type);
        }

        public void RegisterTypes(params Type[] types)
        {
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
            RegisterType(typeof(T));
        }

        public void RegisterDirective(DirectiveGraphType directive)
        {
            _directives.Add(directive ?? throw new ArgumentNullException(nameof(directive)));
        }

        public void RegisterDirectives(IEnumerable<DirectiveGraphType> directives)
        {
            foreach (var directive in directives)
                RegisterDirective(directive);
        }

        public void RegisterDirectives(params DirectiveGraphType[] directives)
        {
            foreach (var directive in directives)
                RegisterDirective(directive);
        }

        public DirectiveGraphType FindDirective(string name)
        {
            return _directives.FirstOrDefault(x => x.Name == name);
        }

        public void RegisterValueConverter(IAstFromValueConverter converter)
        {
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

            return _lookup.Value[name];
        }

        public void Dispose()
        {
            Services = null;
            Query = null;
            Mutation = null;
            Subscription = null;
            _additionalInstances.Clear();
            _additionalTypes.Clear();

            if (_lookup.IsValueCreated)
            {
                _lookup.Value.Clear();
            }
        }

        private void RegisterType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!typeof(IGraphType).IsAssignableFrom(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Type must be of GraphType.");
            }

            if (!_additionalTypes.Contains(type))
            {
                _additionalTypes.Add(type);
            }
        }

        private GraphTypesLookup CreateTypesLookup()
        {
            var resolvedTypes = _additionalTypes
                .Select(t => Services.GetRequiredService(t.GetNamedType()) as IGraphType)
                .ToList();

            var types = _additionalInstances.Union(
                    new IGraphType[]
                    {
                        Query,
                        Mutation,
                        Subscription
                    })
                .Union(resolvedTypes)
                .Where(x => x != null)
                .ToList();

            return GraphTypesLookup.Create(
                types,
                _directives,
                type => Services.GetRequiredService(type) as IGraphType,
                FieldNameConverter);
        }
    }
}
