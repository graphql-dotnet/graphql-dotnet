using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Conversion;
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
    }

    public class Schema : ISchema
    {
        private readonly Lazy<GraphTypesLookup> _lookup;
        private readonly List<Type> _additionalTypes;
        private readonly List<IGraphType> _additionalInstances;
        private readonly List<DirectiveGraphType> _directives;

        public Schema()
            : this(type => (GraphType) Activator.CreateInstance(type))
        {
        }

        public Schema(Func<Type, IGraphType> resolveType)
        {
            ResolveType = resolveType;

            _lookup = new Lazy<GraphTypesLookup>(CreateTypesLookup);
            _additionalTypes = new List<Type>();
            _additionalInstances = new List<IGraphType>();
            _directives = new List<DirectiveGraphType>
            {
                DirectiveGraphType.Include,
                DirectiveGraphType.Skip,
                DirectiveGraphType.Deprecated
            };
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
            FindType("__abcd__");
        }

        public IObjectGraphType Query { get; set; }

        public IObjectGraphType Mutation { get; set; }

        public IObjectGraphType Subscription { get; set; }

        public Func<Type, IGraphType> ResolveType { get; set; }

        public IEnumerable<DirectiveGraphType> Directives
        {
            get
            {
                return _directives;
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                _directives.Clear();
                _directives.Fill(value);
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
            _additionalInstances.Add(type);
        }

        public void RegisterTypes(params IGraphType[] types)
        {
            _additionalInstances.AddRange(types);
        }

        public void RegisterTypes(params Type[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            types.Apply(RegisterType);
        }

        public void RegisterType<T>() where T : IGraphType
        {
            RegisterType(typeof(T));
        }

        public void RegisterDirective(DirectiveGraphType directive)
        {
            _directives.Add(directive);
        }

        public void RegisterDirectives(params DirectiveGraphType[] directives)
        {
            directives.Apply(RegisterDirective);
        }

        public DirectiveGraphType FindDirective(string name)
        {
            return _directives.FirstOrDefault(x => x.Name == name);
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
            ResolveType = null;
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
            if (!typeof (IGraphType).IsAssignableFrom(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Type must be of GraphType.");
            }

            _additionalTypes.Fill(type);
        }

        private GraphTypesLookup CreateTypesLookup()
        {
            var resolvedTypes = _additionalTypes.Select(t => ResolveType(t.GetNamedType())).ToList();

            var types = _additionalInstances.Concat(
                    new IGraphType[]
                    {
                        Query,
                        Mutation,
                        Subscription
                    })
                .Concat(resolvedTypes)
                .Where(x => x != null)
                .ToList();

            return GraphTypesLookup.Create(types, _directives, ResolveType, FieldNameConverter);
        }
    }
}
