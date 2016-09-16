using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GraphQL.Types
{
    public interface ISchema : IDisposable
    {
        IObjectGraphType Query { get; set; }

        IObjectGraphType Mutation { get; set; }

        IObjectGraphType Subscription { get; set; }

        IEnumerable<DirectiveGraphType> Directives { get; set; }

        IEnumerable<IGraphType> AllTypes { get; }

        IGraphType FindType(string name);

        IGraphType FindType(Type type);

        IEnumerable<IGraphType> FindTypes(IEnumerable<Type> types);

        IEnumerable<IGraphType> FindImplementationsOf(Type type);

        IEnumerable<Type> AdditionalTypes { get; }

        void RegisterTypes(params Type[] types);

        void RegisterType<T>() where T : IGraphType;
    }

    public class Schema : ISchema
    {
        private readonly Lazy<GraphTypesLookup> _lookup;
        private readonly List<Type> _additionalTypes;
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
            _directives = new List<DirectiveGraphType>
            {
                DirectiveGraphType.Include,
                DirectiveGraphType.Skip,
                DirectiveGraphType.Deprecated
            };
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

        public IEnumerable<IGraphType> AllTypes
        {
            get
            {
                return _lookup
                    .Value
                    .All()
                    .ToList();
            }
        }

        public IEnumerable<Type> AdditionalTypes => _additionalTypes;

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

        public IGraphType FindType(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentOutOfRangeException(nameof(name), "A type name is required to lookup.");
            }

            return _lookup.Value[name];
        }

        public IGraphType FindType(Type type)
        {
            if (type == null)
            {
                return null;
            }

            if (type.GetTypeInfo().IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(ListGraphType<>) || genericDef == typeof(NonNullGraphType<>))
                {
                    return (GraphType) Activator.CreateInstance(type);
                }
            }

            return _lookup.Value[type] ?? AddType(type);
        }

        public IEnumerable<IGraphType> FindTypes(IEnumerable<Type> types)
        {
            return types.Select(FindType).ToList();
        }

        public IEnumerable<IGraphType> FindImplementationsOf(Type type)
        {
            return _lookup.Value.FindImplemenationsOf(type);
        }

        public void Dispose()
        {
            ResolveType = null;
            Query = null;
            Mutation = null;
            Subscription = null;

            if (_lookup.IsValueCreated)
            {
                _lookup.Value.Clear();
            }
        }

        private void RegisterType(Type type)
        {
            if (!typeof (GraphType).GetTypeInfo().IsAssignableFrom(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Type must be of GraphType.");
            }

            _additionalTypes.Fill(type);
        }

        private GraphTypesLookup CreateTypesLookup()
        {
            var resolvedTypes = _additionalTypes.Select(t => ResolveType(t.GetNamedType())).ToList();

            var types = new List<IGraphType>
            {
                Query,
                Mutation,
                Subscription
            }
            .Concat(resolvedTypes)
            .Where(x => x != null)
            .ToList();

            return GraphTypesLookup.Create(types, ResolveType);
        }

        private IGraphType AddType(Type type)
        {
            if (type == null)
            {
                return null;
            }

            var ctx = new TypeCollectionContext(ResolveType, (name, graphType, context) =>
            {
                _lookup.Value.AddType(graphType, context);
            });

            var namedType = type.GetNamedType();
            var instance = ResolveType(namedType);
            _lookup.Value.AddType(instance, ctx);
            return instance;
        }
    }
}
